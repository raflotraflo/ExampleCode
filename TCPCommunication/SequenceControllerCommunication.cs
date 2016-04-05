using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using PTL.Alarms; //wszystkei alarmy powoduja błąd bo brakuje biblioteki


namespace TCPCommunication
{
    [Serializable]
    public class SequenceControllerCommunication : ISequenceControllerCommunicationData, ISequenceControllerForVisuControl
    {
        public event EventHandler<SequenceControllerCommunicationDataEventArgs> DeleteOrder;
        public event EventHandler<SequenceControllerCommunicationDataEventArgs> InsertOrder;
        public event EventHandler SendDataChanged;
        public event EventHandler ReceiveDataChanged;
        public event EventHandler ErrorChanged;
        public event EventHandler EnableChanged;
        public event EventHandler SequenceControllerStopped;

        private TcpClient _client;
        private IMapper<byte[], ReceiveDataFrame> _receiveMapper;
        private IMapper<SendDataFrame, byte[]> _sendMapper;
        private ReceiveDataFrame _lastReceiveData;
        private SendDataFrame _currentSendData;
        private SendDataFrame _lastSendData;
        private IPAddress _ipAddress;
        private SemaphoreSlim _semaphoreSend;
        private SemaphoreSlim _semaphoreEnable;
        private SemaphoreSlim _semaphoreReconnect;
        private Task _receiveDataTask;
        private bool _alarmConnection;
        private bool _isEnable;
        private int _port;
        private int _bufferSize;
        private CancellationTokenSource _cancellationTokenReciveData;

        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private static System.Timers.Timer _timerCounter;
        private const int TimerCounter = 1000;           //time in milliseconds
        private int _noConnectionTime = 0;               //time in seconds
        private const int ConnectionAlarmTimeout = 20;  //time in seconds
        private const int ConnectionTimeout = 25;       //time in seconds


        private ErrorBitsFromSequenceControler _errorBits = new ErrorBitsFromSequenceControler();


        private Alarm _sequenceControllerCommunication_Stopped;
        private Alarm _sequenceControllerCommunication_NoConnection;

        public SequenceControllerCommunication(string ip, int port)
        {

            _receiveMapper = new ReceiveDataFrameMapper();
            _sendMapper = new SendDataFrameMapper();

            _lastReceiveData = new ReceiveDataFrame();
            _currentSendData = new SendDataFrame();
            _lastSendData = new SendDataFrame();

            _semaphoreSend = new SemaphoreSlim(1, 1);
            _semaphoreEnable = new SemaphoreSlim(1, 1);
            _semaphoreReconnect = new SemaphoreSlim(1, 1);

            _bufferSize = ReceiveDataFrame.NumberOfBytes;


            _sequenceControllerCommunication_Stopped = new Alarms.Alarm(Alarms.AlarmType.Warning);
            _sequenceControllerCommunication_Stopped.Info.Name = "Balancing Stopped";
            _sequenceControllerCommunication_Stopped.Info.DeviceType = "Balancing";

            _sequenceControllerCommunication_NoConnection = new Alarms.Alarm();
            _sequenceControllerCommunication_NoConnection.Info.Name = "Balancing NoConnection";
            _sequenceControllerCommunication_NoConnection.Info.DeviceType = "Balancing";


            _timerCounter = new System.Timers.Timer(TimerCounter);
            _timerCounter.AutoReset = true;
            _timerCounter.Elapsed += TimerCounter_Elapsed;
            GC.KeepAlive(_timerCounter);

            _port = port;
            _ipAddress = IPAddress.Parse(ip);
            _sequenceControllerCommunication_Stopped.Rise("Stopped");

        }

        public async Task SaveAsync(IPAddress ipAddress, int port)
        {
            bool isEnable = _isEnable;

            await DisableAsync().ConfigureAwait(false);

            await _semaphoreReconnect.WaitAsync();

            _ipAddress = ipAddress;
            _port = port;

            _semaphoreReconnect.Release();

            if (isEnable)
            {
                await EnableAsync().ConfigureAwait(false);
            }
        }

        public async Task EnableAsync()
        {
            await _semaphoreEnable.WaitAsync();
            try
            {
                if (_cancellationTokenReciveData == null)
                {

                    _logger.Debug("Connect(ipAddress ={0}, port={1}", _ipAddress.ToString(), _port);
                    //_noConnectionTime = 0;
                    _cancellationTokenReciveData = new CancellationTokenSource();

                    await ChangeEnable(true).ConfigureAwait(false);

                    //await ReconnectAsync(true).ConfigureAwait(false);

                    _noConnectionTime = ConnectionTimeout - 1;
                    _timerCounter.Start();

                    _receiveDataTask = Task.Factory.StartNew(() => ReceiveAsync(_cancellationTokenReciveData.Token)
                                                    , _cancellationTokenReciveData.Token
                                                    , TaskCreationOptions.LongRunning
                                                    , TaskScheduler.Default);


                    _logger.Debug("_task.Start(await ReceiveAsync(_cancellationTokenReciveData.Token))");
                }
            }
            finally
            {
                _semaphoreEnable.Release();
            }
        }

        public async Task DisableAsync()
        {
            await _semaphoreEnable.WaitAsync();
            try
            {
                var cancellationTokenSourceHelper = _cancellationTokenReciveData;
                if (cancellationTokenSourceHelper != null)
                {
                    cancellationTokenSourceHelper.Cancel();
                    await _receiveDataTask.ConfigureAwait(false);
                    cancellationTokenSourceHelper.Dispose();

                    if (_client != null)
                    {
                        _client.Close();
                    }

                    await ChangeEnable(false).ConfigureAwait(false);
                    await ChangeAlarmConnection(false).ConfigureAwait(false);
                    _cancellationTokenReciveData = null;

                    _timerCounter.Stop();
                    _logger.Info("Disconnect()");

                    _sequenceControllerCommunication_NoConnection.Fall();
                    OnSequenceControllerStopped();
                    ClearErrors();
                    OnErrorChanged();
                }
            }
            finally
            {
                _semaphoreEnable.Release();
            }
        }

        private async Task ReconnectAsync()
        {
            await _semaphoreReconnect.WaitAsync();

            try
            {

                while (_isEnable)
                {
                    bool connectionWasUnsuccesfull = false;
                    try
                    {
                        if (_client != null)
                        {
                            _client.Close();
                        }
                        _client = new TcpClient();

                        _logger.Debug("_client.Connect(ipAddress ={0}, port={1}", _ipAddress.ToString(), _port);

                        await _client.ConnectAsync(_ipAddress, _port).ConfigureAwait(false);

                        await ChangeAlarmConnection(false).ConfigureAwait(false);
                        _noConnectionTime = 0;

                        _logger.Info("Connected to {0},{1}", _ipAddress, _port);

                        return;

                    }
                    catch (Exception exp)
                    {
                        connectionWasUnsuccesfull = true;
                        _logger.Error("Error while connecting to ({0}:{1}). {2}.", _ipAddress, _port, exp);
                    }

                    if (connectionWasUnsuccesfull)
                    {
                        await ChangeAlarmConnection(true).ConfigureAwait(false);
                        await Task.Delay(3000).ConfigureAwait(false);
                    }

                }
            }
            finally
            {
                _semaphoreReconnect.Release();
            }
        }

        private async void TimerCounter_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timerCounter.Stop();
            try
            {
                Interlocked.Increment(ref _noConnectionTime);

                bool isConnectionError = (_noConnectionTime >= ConnectionAlarmTimeout);
                await ChangeAlarmConnection(isConnectionError);

                bool reconnect = (_noConnectionTime % ConnectionTimeout == 0);
                if (reconnect)
                {
                    _logger.Debug("_connectionCounter = {0}, Call ReConnect()", _noConnectionTime);
                    await ReconnectAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                if (_isEnable)
                {
                    _timerCounter.Start();
                }
            }
        }

        private async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    bool wait = false;
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        byte[] buffer = new byte[_bufferSize];
                        int bytesRead = 0;


                        bytesRead = await _client.GetStream().ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);


                        if (bytesRead <= 0)
                        {
                            continue;
                        }

                        ReceiveDataFrame receiveData = _receiveMapper.Map(buffer);

                        _currentSendData.LifeBit = receiveData.LifeBit;

                        _logger.Debug("Receive frame, length = {0}, lifeBbit={1} ", bytesRead, receiveData.LifeBit);

                        bool isNewRequest = false;

                        if (receiveData.LifeBit != _lastReceiveData.LifeBit)
                        {
                            Interlocked.Exchange(ref _noConnectionTime, 0);
                        }

                        try
                        {
                            isNewRequest = IsNewRequest(receiveData, _lastReceiveData);
                        }
                        catch (Exception e)
                        {
                            isNewRequest = false;
                            _currentSendData.UnknownError = true;
                            _logger.Fatal("Error in received frame: Insert and Delete are true - ");
                        }

                        if (isNewRequest)
                        {
                            CallEvent(receiveData);
                        }

                        _lastReceiveData = receiveData.GetCopy();
                        OnReceiveDataChanged();

                        try
                        {
                            _logger.Debug("Send frame from Task ReceiveAsync()");
                            await _semaphoreSend.WaitAsync();

                            if (isNewRequest)
                            {
                                receiveData.ChangeSendDataFrame(_currentSendData); //rch osobna klasa 
                            }

                            await SendAsync(_currentSendData).ConfigureAwait(false);

                        }
                        catch (SendingFailedException)
                        {
                            _logger.Error("Error while send frame from Task ReceiveAsync()");
                        }
                        finally
                        {
                            _semaphoreSend.Release(); //semaphore do finally
                        }


                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exp)
                    {
                        _logger.Error("Error in Task ReceiveAsync() error: {0}", exp);
                        wait = true;
                    }

                    if (wait)
                    {
                        await Task.Delay(2000);
                        wait = false;
                    }
                }
            }
            finally
            {
                _cancellationTokenReciveData = null;
            }
        }

        private bool IsNewRequest(ReceiveDataFrame newframe, ReceiveDataFrame oldFrame)
        {
            _logger.Debug("Check if received a new frame");

            if (newframe.Insert == true && newframe.Delete == true)
            {
                throw new Exception("Error in received frame: Insert and Delete are true");
            }

            if (newframe.Insert == false && newframe.Delete == false)
            {
                return true;
            }

            if (newframe.Delete != oldFrame.Delete)
            {
                return true;
            }

            if (newframe.Insert != oldFrame.Insert)
            {
                return true;
            }

            if (newframe.LBHD != oldFrame.LBHD)
            {
                if (newframe.Insert || newframe.Delete)
                {
                    return true;
                }
            }


            return false;
        }

        private void CallEvent(ReceiveDataFrame receiveData)
        {
            _logger.Debug("New frame");
            SequenceControllerCommunicationDataEventArgs eventArgs = new SequenceControllerCommunicationDataEventArgs();
            eventArgs.LBHD = receiveData.LBHD;
            eventArgs.BalancingDate = receiveData.BalanceDate;

            if (receiveData.Delete)
            {
                OnDeleteOrder(eventArgs);
            }
            if (receiveData.Insert)
            {
                OnInsertOrder(eventArgs);
            }
        }

        private async Task SendAsync(SendDataFrame frame)
        {

            bool isConnected = false;
            var client = _client;
            if (client != null)
            {
                isConnected = client.Connected;
            }

            if (isConnected)
            {
                byte[] data = _sendMapper.Map(frame);

                try
                {

                    await _client.GetStream().WriteAsync(data, 0, data.Length).ConfigureAwait(false);

                    _logger.Debug("Send frame: length={0}, lifBit={1}, lbhd={2}", data.Length, frame.LifeBit, frame.LBHD);
                }
                catch (Exception)
                {
                    _logger.Error("Error while sending frame: length={0}, lifBit={1}, lbhd={2}", data.Length, frame.LifeBit, frame.LBHD);
                    //throw new SendingFailedException("SendingFailed.");
                }

                if (IsErrorChanged(frame, _lastSendData))
                {
                    OnErrorChanged();
                }

                _lastSendData = frame.GetCopy();
                OnSendDataChanged();
            }
            else
            {
                _logger.Error("Without connection while sending frame: lifBit={1}, lbhd={2}", frame.LifeBit, frame.LBHD);
                //throw new SendingFailedException("Sending error. No connection.");
            }

        }

        private bool IsErrorChanged(SendDataFrame newframe, SendDataFrame oldFrame)
        {
            _logger.Debug("Check if sended a new error");


            if (newframe.NotFound != oldFrame.NotFound)
            {
                return true;
            }
            if (newframe.SOAP != oldFrame.SOAP)
            {
                return true;
            }
            if (newframe.NotInsert != oldFrame.NotInsert)
            {
                return true;
            }
            if (newframe.NotDelete != oldFrame.NotDelete)
            {
                return true;
            }
            if (newframe.UnknownError != oldFrame.UnknownError)
            {
                return true;
            }


            return false;
        }

        private void OnDeleteOrder(SequenceControllerCommunicationDataEventArgs eventArgs)
        {
            _logger.Debug("Received frame: Delete order: {0}, BalancingData: {1}", eventArgs.LBHD, eventArgs.BalancingDate);
            if (DeleteOrder != null)
            {
                DeleteOrder(this, eventArgs);
            }
        }
        private void OnInsertOrder(SequenceControllerCommunicationDataEventArgs eventArgs)
        {
            _logger.Debug("Received frame: Insert order: {0}, BalancingData: {1}", eventArgs.LBHD, eventArgs.BalancingDate);
            if (InsertOrder != null)
            {
                InsertOrder(this, eventArgs);
            }
        }
        private void OnSendDataChanged()
        {
            _logger.Debug("Changed sending frame OnSendDataChanged()");

            if (SendDataChanged != null)
            {
                SendDataChanged(this, EventArgs.Empty);
            }
        }
        private void OnReceiveDataChanged()
        {
            _logger.Debug("Changed receiving frame OnReceiveDataChanged()");

            if (ReceiveDataChanged != null)
            {
                ReceiveDataChanged(this, EventArgs.Empty);
            }
        }
        private void OnErrorChanged()
        {
            _logger.Debug("Call error OnErrorChanged()");
            _errorBits.ConnectionAlarm = _alarmConnection;
            _errorBits.LBHDNotFound = _currentSendData.NotFound;
            _errorBits.UnknownError = _currentSendData.UnknownError;
            _errorBits.OrderNotDeleted = _currentSendData.NotDelete;
            _errorBits.OrderNotInserted = _currentSendData.NotInsert;
            _errorBits.OrderWithSOP = _currentSendData.SOAP;

            if (ErrorChanged != null)
            {
                ErrorChanged(this, EventArgs.Empty);
            }
        }

        private void OnEnableChanged()
        {
            if (EnableChanged != null)
            {
                EnableChanged(this, EventArgs.Empty);
            }
        }

        private void ClearErrors()
        {
            _alarmConnection = false;
            _currentSendData.NotFound = false;
            _currentSendData.UnknownError = false;
            _currentSendData.NotDelete = false;
            _currentSendData.NotInsert = false;
            _currentSendData.SOAP = false;
        }

        private async Task ChangeAlarmConnection(bool isActive)
        {
            if (_alarmConnection != isActive)
            {
                _logger.Debug("Change status connection: ", isActive);
                await Task.Run(() =>
                {
                    _alarmConnection = isActive;
                    if (_alarmConnection)
                    {
                        _sequenceControllerCommunication_NoConnection.Rise(String.Format("NoConnection to {0}, port {1}", _ipAddress, _port));
                    }
                    else
                    {
                        _sequenceControllerCommunication_NoConnection.Fall();
                    }
                }).ConfigureAwait(false);
                OnErrorChanged();
            }
        }

        private async Task ChangeEnable(bool enable)
        {
            if (_isEnable != enable)
            {
                await Task.Run(() =>
                {
                    _isEnable = enable;
                    if (_isEnable)
                    {
                        _sequenceControllerCommunication_Stopped.Fall();
                    }
                    else
                    {
                        _sequenceControllerCommunication_Stopped.Rise("Stopped");
                    }

                }).ConfigureAwait(false);

                OnEnableChanged();
            }
        }


        #region ISequenceControllerCommunicationData

        public async Task LBHDnotFoundAsync(string lbhd)
        {
            try
            {
                _logger.Debug("Call method: LBHDnotFound({0})", lbhd);
                await _semaphoreSend.WaitAsync().ConfigureAwait(false);
                _currentSendData.NotFound = true;
                _currentSendData.LBHD = lbhd;

                await SendAsync(_currentSendData).ConfigureAwait(false);
            }
            catch
            {
                _logger.Error("Error while send frame from Task LBHDnotFoundAsync({0})", lbhd);
            }
            finally
            {
                _semaphoreSend.Release();
            }
        }

        public async Task OrderWithSOPAsync(string lbhd)
        {
            try
            {
                _logger.Debug("Call method: OrderWithSOPAsync({0},)", lbhd);
                await _semaphoreSend.WaitAsync().ConfigureAwait(false);
                _currentSendData.SOAP = true;
                _currentSendData.LBHD = lbhd;

                await SendAsync(_currentSendData).ConfigureAwait(false);

            }
            catch
            {
                _logger.Error("Error while send frame from Task OrderWithSOPAsync({0})", lbhd);
            }
            finally
            {
                _semaphoreSend.Release();
            }
        }

        public async Task OrderInsertedAsync(string lbhd)
        {
            try
            {
                _logger.Debug("Call method: OrderInsertedAsync({0},)", lbhd);

                await _semaphoreSend.WaitAsync().ConfigureAwait(false);
                if (_lastReceiveData.Insert)
                {
                    _currentSendData.InsertAck = true;
                    _currentSendData.LBHD = lbhd;

                    await SendAsync(_currentSendData).ConfigureAwait(false);
                }
            }
            catch
            {
                _logger.Error("Error while send frame from Task OrderInsertedAsync({0})", lbhd);
            }
            finally
            {
                _semaphoreSend.Release();
            }
        }

        public async Task OrderNotInsertedAsync(string lbhd)
        {
            try
            {
                _logger.Debug("Call method: OrderNotInsertedAsync({0})", lbhd);
                await _semaphoreSend.WaitAsync().ConfigureAwait(false);

                _currentSendData.NotInsert = true;
                _currentSendData.LBHD = lbhd;

                await SendAsync(_currentSendData).ConfigureAwait(false);

            }
            catch
            {
                _logger.Error("Error while send frame from Task OrderNotInsertedAsync({0})", lbhd);
            }
            finally
            {
                _semaphoreSend.Release();
            }
        }

        public async Task OrderDeletedAsync(string lbhd)
        {
            try
            {
                _logger.Debug("Call method: OrderDeletedAsync({0})", lbhd);

                await _semaphoreSend.WaitAsync().ConfigureAwait(false);
                if (_lastReceiveData.Delete)
                {
                    _currentSendData.DeleteAck = true;
                    _currentSendData.LBHD = lbhd;

                    await SendAsync(_currentSendData).ConfigureAwait(false);
                }
            }
            catch
            {
                _logger.Error("Error while send frame from Task OrderDeletedAsync({0})", lbhd);
            }
            finally
            {
                _semaphoreSend.Release();
            }

        }

        public async Task OrderNotDeletedAsync(string lbhd)
        {
            try
            {
                _logger.Debug("Call method: OrderNotDeletedAsync({0})", lbhd);
                await _semaphoreSend.WaitAsync().ConfigureAwait(false);

                _currentSendData.NotDelete = true;
                _currentSendData.LBHD = lbhd;

                await SendAsync(_currentSendData).ConfigureAwait(false);
            }
            catch
            {
                _logger.Error("Error while send frame from Task OrderNotDeletedAsync({0})", lbhd);
            }
            finally
            {
                _semaphoreSend.Release();
            }
        }

        public async Task UnknownErrorAsync()
        {
            try
            {
                _logger.Debug("Call method: UnknownErrorAsync()");
                await _semaphoreSend.WaitAsync().ConfigureAwait(false);

                _currentSendData.UnknownError = true;

                await SendAsync(_currentSendData).ConfigureAwait(false);

            }
            catch
            {
                _logger.Error("Error while send frame from Task UnknownErrorAsync()");
            }
            finally
            {
                _semaphoreSend.Release();
            }
        }

        public void OnSequenceControllerStopped()
        {
            if (SequenceControllerStopped != null)
            {
                SequenceControllerStopped(this, EventArgs.Empty);
            }
        }

        #endregion


        #region ISequenceControllerForVisuControl

        SendDataFrame ISequenceControllerForVisuControl.SendDataBits
        {
            get
            {
                return _lastSendData;
            }
        }

        ReceiveDataFrame ISequenceControllerForVisuControl.ReceiveDataBits
        {
            get
            {
                return _lastReceiveData;
            }
        }

        ErrorBitsFromSequenceControler ISequenceControllerForVisuControl.ErrorBits
        {
            get
            {
                return _errorBits;
            }
        }

        IPAddress ISequenceControllerForVisuControl.IP
        {
            get
            {
                return _ipAddress;
            }
        }

        int ISequenceControllerForVisuControl.Port
        {
            get
            {
                return _port;
            }
        }

        bool ISequenceControllerForVisuControl.Enabled
        {
            get
            {
                return _isEnable;
            }
        }

        #endregion


        public void OnInsertOrder(string lbhd)// do testow
        {
            SequenceControllerCommunicationDataEventArgs BEA = new SequenceControllerCommunicationDataEventArgs();
            BEA.LBHD = lbhd;
            BEA.BalancingDate = DateTime.Now;
            if (InsertOrder != null)
            {
                InsertOrder(this, BEA);
            }
        }
        public void OnDeleteOrder(string lbhd)// do testow
        {
            SequenceControllerCommunicationDataEventArgs BEA = new SequenceControllerCommunicationDataEventArgs();
            BEA.LBHD = lbhd;
            BEA.BalancingDate = DateTime.Now;
            if (DeleteOrder != null)
            {
                DeleteOrder(this, BEA);
            }
        }
    }
}

