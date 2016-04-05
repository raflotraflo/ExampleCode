using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Drawing;
using System.Net;
using GalaSoft.MvvmLight;
using System.Xml;
using System.Xml.Schema;
using TraceabilityScansReplacement.Domain;
using TraceabilityScansReplacement.Domain.Data;
using TraceabilityScansReplacement.Domain.Data.DataDefinition;
using TraceabilityScansReplacement.Domain.Factory;
using TraceabilityScansReplacement.Domain.Model;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using TraceabilityScansReplacement.UI.Model;
using TraceabilityScansReplacement.UI.ExtensionMethods;
using System.Windows.Input;

namespace MvvmAndAutofac.ViewModel
{
    public class ScanDefinitionViewModel : ViewModelBase
    {
        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        IScanDefinitionRepository _scanDefinitionRepository;
        IScanTypeNameRepository _scanTypeNameRepository;
        bool _canExecuteSaveDeleteCommand = false;
        public Task Initialization { get; private set; }
        public List<ScanValuePart> Template { get; set; }


        public ScanDefinitionViewModel(IScanDefinitionRepository scanDefinitionRepository, IScanTypeNameRepository scanTypeNameRepository, IEnumerable<ScanValuePart> template)
        {
            SaveScanDefinitionCommand = new RelayCommand<ScanDefinition>(async (x) => await SaveScanDefinitionAsync(x), (x) => _canExecuteSaveDeleteCommand);
            AddScanDefinitionCommand = new RelayCommand<ScanDefinition>(async (x) => await AddScanDefinitionAsync(x));
            DeleteScanDefinitionCommand = new RelayCommand<ScanDefinition>(async (x) => await DeleteScanDefinitionAsync(x), (x) => _canExecuteSaveDeleteCommand);
            RefreshScanDefinitionCommand = new RelayCommand(async () => await RefreshAsync());
            SearchCommand = new RelayCommand<object>((x) => Search(x));
            UpdateScanTypeNamesCommand = new RelayCommand(async () => await UpdateScanTypeNamesAsync());
            ClearScanDefinitionCommand = new RelayCommand(() => ClearScanDefinition());
            ToggleButtonPropertyCollectionChangedCommand = new RelayCommand(() => ToggleButtonPropertyCollectionChanged());

            _scanDefinitionRepository = scanDefinitionRepository;
            _scanTypeNameRepository = scanTypeNameRepository;
            Template = template.ToList();

            Initialization = RefreshAsync();

            //ToggleButtonProperty.CollectionChanged += ToggleButtonProperty_CollectionChanged;

            SelectedScanDefinition = new ScanDefinition() { ID = 0, OrderMask = "", ScanMask = "" };
            //DescriptionListFiltr = new ObservableCollection<int> { 1, 2, 3, 4, 5, 6 };
        }

        private void ToggleButtonPropertyCollectionChanged()
        {
            Description = ToggleButtonProperty.BitArrayToInt();
        }

        void SelectScanDefinition(ScanDefinition sd)
        {
            if (sd != null)
            {
                try
                {

                    ToggleButtonProperty = new ObservableCollection<bool>(sd.Description.IntToBitArray());
                    OrderMask = sd.OrderMask.StringToCharCollection(60);
                    ScanMask = sd.ScanMask.StringToCharCollection(65);
                    ScanArchiveLength = sd.ScanArchiveLength;
                    ScanArchiveOffset = sd.ScanArchiveOffset;
                    Length = sd.Lenght;
                    SelectedScanDefinitionType = ScanTypeList.Where(x => x.ID == sd.Type).First();
                }
                catch (Exception) { }
            }
            else
            {
                ToggleButtonProperty = new ObservableCollection<bool>(0.IntToBitArray());
                OrderMask = "".StringToCharCollection(60);
                ScanMask = "".StringToCharCollection(65);
                SelectedScanDefinitionType = ScanTypeList.First();
            }
        }

        void Search(object o)
        {
            if (o != null)
            {
                var c = o.GetType();
                string a = o.ToString();
                if (a == "1")
                {
                    if (SelectedScanType != null && SelectedScanType.ID != -1)
                    {
                        ScanDefinitionList = new ObservableCollection<ScanDefinition>(_scanDefinitionListOriginal.Where(x => x.Type == SelectedScanType.ID));
                    }
                    else
                    {
                        ScanDefinitionList = _scanDefinitionListOriginal;
                    }
                    //aktualiozacja SelectedDescriptionFiltr
                    //UpdateDescriptionListFiltr();
                }
                //else if (a == "2" && SelectedDescriptionFiltr != null)
                //{
                //    if (SelectedScanType != null && SelectedScanType.ID != -1)
                //    {
                //        if (SelectedDescriptionFiltr != -1)
                //            ScanDefinitionList = new ObservableCollection<ScanDefinition>(_scanDefinitionListOriginal.Where(x => x.Type == SelectedScanType.ID && x.Description == SelectedDescriptionFiltr));
                //        else
                //            ScanDefinitionList = new ObservableCollection<ScanDefinition>(_scanDefinitionListOriginal.Where(x => x.Type == SelectedScanType.ID));
                //    }
                //    else
                //    {
                //        if (SelectedDescriptionFiltr != -1)
                //            ScanDefinitionList = new ObservableCollection<ScanDefinition>(_scanDefinitionListOriginal.Where(x => x.Description == SelectedDescriptionFiltr));
                //        else
                //            ScanDefinitionList = _scanDefinitionListOriginal;

                //    }

                //}

                if (ScanDefinitionList.Any())
                {
                    SelectedScanDefinition = ScanDefinitionList.First();
                }
            }
        }

        void ClearScanDefinition()
        {
            ToggleButtonProperty = new ObservableCollection<bool>(0.IntToBitArray());
            OrderMask = "".StringToCharCollection(60);
            ScanMask = "".StringToCharCollection(65);
            SelectedScanDefinitionType = ScanTypeList.First();
        }

        private ScanDefinition SaveScan(ScanDefinition sd)
        {
            sd.Lenght = Length;
            sd.ScanArchiveLength = ScanArchiveLength;
            sd.ScanArchiveOffset = ScanArchiveOffset;
            sd.Description = ToggleButtonProperty.BitArrayToInt();
            sd.ScanMask = ScanMask.CollectionCharToString();
            sd.OrderMask = OrderMask.CollectionCharToString();
            sd.Type = SelectedScanDefinitionType.ID;
            return sd;
        }

        void ButtonCanExecuteChanged()
        {
            if (SelectedScanDefinition == null)
            {
                _canExecuteSaveDeleteCommand = false;
            }
            else
            {
                if (SelectedScanDefinition.ID == 0)
                    _canExecuteSaveDeleteCommand = false;
                else
                    _canExecuteSaveDeleteCommand = true;
            }

            SaveScanDefinitionCommand.RaiseCanExecuteChanged();
            DeleteScanDefinitionCommand.RaiseCanExecuteChanged();
        }

        //void UpdateDescriptionListFiltr()
        //{
        //    DescriptionListFiltr = new ObservableCollection<int>();
        //    DescriptionListFiltr.Add(-1);
        //    foreach (ScanDefinition sd in ScanDefinitionList)
        //    {
        //        if (!DescriptionListFiltr.Contains(sd.Description))
        //            DescriptionListFiltr.Add(sd.Description);
        //    }

        //    if (DescriptionListFiltr.Any())
        //    {
        //        SelectedDescriptionFiltr = DescriptionListFiltr.First();
        //    }
        //}

        async Task RefreshAsync()
        {
            ScanType temporarySelectedScanType = SelectedScanType;

            IEnumerable<ScanDefinition> b = await _scanDefinitionRepository.GetAllAsync();
            _scanDefinitionListOriginal = new ObservableCollection<ScanDefinition>(b);
            ScanDefinitionList = _scanDefinitionListOriginal;

            //UpdateDescriptionListFiltr();

            IEnumerable<ScanType> c = await _scanTypeNameRepository.GetAllAsync();
            ScanTypeFiltrList = new ObservableCollection<ScanType>(c);
            ScanTypeList = new ObservableCollection<ScanType>(c);
            ScanTypeFiltrList.Insert(0, new ScanType() { ID = -1, Name = "Wszystkie" });

            if (temporarySelectedScanType == null)
            {
                SelectedScanType = ScanTypeFiltrList[0];
            } else
            {
                try
                {
                    SelectedScanType = ScanTypeFiltrList.Where(x => x.ID == temporarySelectedScanType.ID).First();
                }
                catch (Exception) { }
            }
        }

        async Task UpdateScanTypeNamesAsync()
        {
            try
            {
                await _scanTypeNameRepository.UpdateFromExternalSourceAsync();
                await RefreshAsync();
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Error in ScanDefinitionViewModel.UpdateScanTypeNamesAsync {0}", e);
            }
        }

        private async Task SaveScanDefinitionAsync(ScanDefinition s)
        {
            MessageBoxResult result = MessageBox.Show("Czy chcesz zapisać skan?", "Zapisywanie", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    s = SaveScan(s);
                    await _scanDefinitionRepository.SaveAsync(s);
                    await RefreshAsync();
                }
                catch (Exception e)
                {
                    _logger.Fatal(e, "Error in ScanDefinitionViewModel.SaveScanDefinitionAsync {0}", e);
                }
            }
        }

        private async Task AddScanDefinitionAsync(ScanDefinition s)
        {
            MessageBoxResult result = MessageBox.Show("Czy chcesz dodać skan?", "Dodawanie", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (SelectedScanDefinitionType == null)
                    {
                        MessageBox.Show("Wybierz Typ!");
                    }
                    else
                    {
                        s = SaveScan(s);
                        s.ID = 0;
                        await _scanDefinitionRepository.SaveAsync(s);
                        await RefreshAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.Fatal(e, "Error in ScanDefinitionViewModel.AddScanDefinitionAsync {0}", e);
                }
            }
        }

        private async Task DeleteScanDefinitionAsync(ScanDefinition s)
        {
            MessageBoxResult result = MessageBox.Show("Czy chcesz usunąć skan?", "Usuwanie", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _scanDefinitionRepository.DeleteAsync(s);
                    await RefreshAsync();
                }
                catch (Exception e)
                {
                    _logger.Fatal(e, "Error in ScanDefinitionViewModel.DeleteScanDefinitionAsync {0}", e);
                }
            }
        }

        ObservableCollection<ScanDefinition> _scanDefinitionListOriginal;
        ObservableCollection<ScanDefinition> _scanDefinitionList;
        public ObservableCollection<ScanDefinition> ScanDefinitionList
        {
            get
            {
                return _scanDefinitionList;
            }

            set
            {
                if (_scanDefinitionList != value)
                {
                    _scanDefinitionList = value;
                    RaisePropertyChanged(() => ScanDefinitionList);
                }
            }
        }

        ScanDefinition _selectedScanDefinition;
        public ScanDefinition SelectedScanDefinition
        {
            get
            {
                return _selectedScanDefinition;
            }

            set
            {
                if (_selectedScanDefinition != value)
                {
                    if (value == null)
                    {
                        _selectedScanDefinition = new ScanDefinition() { ID = 0, OrderMask = "", ScanMask = "" };
                    }
                    else
                    {
                        _selectedScanDefinition = value;
                    }

                    RaisePropertyChanged(() => SelectedScanDefinition);

                    SelectScanDefinition(_selectedScanDefinition);

                    ButtonCanExecuteChanged();
                }
            }
        }
        //ScanArchiveLength
        int _scanArchiveLength;
        public int ScanArchiveLength
        {
            get
            {
                return _scanArchiveLength;
            }

            set
            {
                if (_scanArchiveLength != value)
                {
                    _scanArchiveLength = value;
                    RaisePropertyChanged(() => ScanArchiveLength);
                }
            }
        }

        int _scanArchiveOffset;
        public int ScanArchiveOffset
        {
            get
            {
                return _scanArchiveOffset;
            }

            set
            {
                if (_scanArchiveOffset != value)
                {
                    _scanArchiveOffset = value;
                    RaisePropertyChanged(() => ScanArchiveOffset);
                }
            }
        }

        int _length;
        public int Length
        {
            get
            {
                return _length;
            }

            set
            {
                if (_length != value)
                {
                    _length = value;
                    RaisePropertyChanged(() => Length);
                }
            }
        }

        int _description;
        public int Description
        {
            get
            {
                return _description;
            }

            set
            {
                if (_description != value)
                {
                    _description = value;
                    RaisePropertyChanged(() => Description);
                }
            }
        }

        //int? _selectedDescriptionFiltr;
        //public int? SelectedDescriptionFiltr
        //{
        //    get
        //    {
        //        return _selectedDescriptionFiltr;
        //    }

        //    set
        //    {
        //        if (_selectedDescriptionFiltr != value)
        //        {
        //            _selectedDescriptionFiltr = value;
        //            RaisePropertyChanged(() => SelectedDescriptionFiltr);
        //        }
        //    }
        //}

        ObservableCollection<int> _descriptionListFiltr = new ObservableCollection<int> { -1 };
        public ObservableCollection<int> DescriptionListFiltr
        {
            get
            {
                return _descriptionListFiltr;
            }

            set
            {
                _descriptionListFiltr = value;
                RaisePropertyChanged(() => DescriptionListFiltr);
            }
        }

        ObservableCollection<ScanType> _scanTypeFiltrList;
        public ObservableCollection<ScanType> ScanTypeFiltrList
        {
            get
            {
                return _scanTypeFiltrList;
            }

            set
            {
                _scanTypeFiltrList = value;
                RaisePropertyChanged(() => ScanTypeFiltrList);
            }
        }

        ScanType _selectedScanType;
        public ScanType SelectedScanType
        {
            get
            {
                return _selectedScanType;
            }

            set
            {
                if (_selectedScanType != value)
                {
                    _selectedScanType = value;
                    RaisePropertyChanged(() => SelectedScanType);

                    //DescriptionListFiltr = new ObservableCollection<int>();
                    //foreach (ScanDefinition sd in ScanDefinitionList)
                    //{
                    //    if (!DescriptionListFiltr.Contains(sd.Type))
                    //        DescriptionListFiltr.Add(sd.Type);
                    //}
                }
            }
        }

        ObservableCollection<ScanType> _scanTypeList;
        public ObservableCollection<ScanType> ScanTypeList
        {
            get
            {
                return _scanTypeList;
            }

            set
            {
                if (_scanTypeList != value)
                {
                    _scanTypeList = value;
                    RaisePropertyChanged(() => ScanTypeList);

                    if (SelectedScanDefinition != null)
                    {
                        SelectedScanDefinitionType = ScanTypeList.Where(x => x.ID == SelectedScanDefinition.Type).First();
                    }
                }
            }
        }


        #region ScanInfo

        ScanType _selectedScanDefinitionType;
        public ScanType SelectedScanDefinitionType
        {
            get
            {
                return _selectedScanDefinitionType;
            }

            set
            {
                if (_selectedScanDefinitionType != value)
                {
                    _selectedScanDefinitionType = value;
                    RaisePropertyChanged(() => SelectedScanDefinitionType);
                }
            }
        }

        ObservableCollection<Boolean> _toggleButtonProperty = new ObservableCollection<bool>();
        public ObservableCollection<Boolean> ToggleButtonProperty
        {
            get
            {
                return _toggleButtonProperty;
            }

            set
            {
                if (_toggleButtonProperty != value)
                {
                    _toggleButtonProperty = value;
                    RaisePropertyChanged(() => ToggleButtonProperty);
                    ToggleButtonPropertyCollectionChanged();
                }
            }
        }


        ObservableCollection<CharValue> _orderMask = new ObservableCollection<CharValue>();
        public ObservableCollection<CharValue> OrderMask
        {
            get
            {
                return _orderMask;
            }

            set
            {
                if (_orderMask != value)
                {
                    _orderMask = value;
                    RaisePropertyChanged(() => OrderMask);
                }
            }
        }


        ObservableCollection<CharValue> _scanMask = new ObservableCollection<CharValue>();
        public ObservableCollection<CharValue> ScanMask
        {
            get
            {
                return _scanMask;
            }

            set
            {
                if (_scanMask != value)
                {
                    _scanMask = value;
                    RaisePropertyChanged(() => ScanMask);
                }
            }
        }

        #endregion

        public RelayCommand<ScanDefinition> SaveScanDefinitionCommand { get; private set; }
        public RelayCommand<ScanDefinition> DeleteScanDefinitionCommand { get; private set; }
        public RelayCommand<ScanDefinition> AddScanDefinitionCommand { get; private set; }
        public RelayCommand<object> SearchCommand { get; private set; }
        public RelayCommand RefreshScanDefinitionCommand { get; private set; }
        public RelayCommand UpdateScanTypeNamesCommand { get; private set; }
        public RelayCommand ClearScanDefinitionCommand { get; private set; }
        public RelayCommand ToggleButtonPropertyCollectionChangedCommand { get; private set; }

    }
}
