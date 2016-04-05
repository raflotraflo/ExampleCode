/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:MvvmAndAutofac"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using Autofac;
using TraceabilityScansReplacement.Domain;
using TraceabilityScansReplacement.Domain.Data;
using TraceabilityScansReplacement.Domain.Data.DataDefinition;
using TraceabilityScansReplacement.Domain.Factory;
using TraceabilityScansReplacement.Domain.Model;
using System.Net;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace MvvmAndAutofac.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public class ViewModelLocator
        {
            /// <summary>
            /// Initializes a new instance of the ViewModelLocator class.
            /// </summary>


            public readonly IContainer container;
            private string ConfigurationFileName = "Application.config";
            private string ConfigurationSchemaFileName = "Configuration.xsd";

            public ViewModelLocator()
            {
                var containerBulider = new ContainerBuilder();

                ReadConfig readConfig = new ReadConfig();

                ConfigData configData = readConfig.ReadConfiguration(ConfigurationFileName, ConfigurationSchemaFileName);

                List<ScanValuePart> _template = new List<ScanValuePart>();
                _template.Add(new ScanValuePart() { Prefix = "[)>06Y", Sufix = "", StartIndex = 8, Length = 14, Color = "Blue" });
                _template.Add(new ScanValuePart() { Prefix = "P", Sufix = "", StartIndex = 24, Length = 8, Color = "Green" });
                _template.Add(new ScanValuePart() { Prefix = "12V", Sufix = "", StartIndex = 36, Length = 9, Color = "Purple" });
                _template.Add(new ScanValuePart() { Prefix = "T", Sufix = "", StartIndex = 47, Length = 16, Color = "Orange" });

                IDataProvider scanner;
                if (configData.Scanner.Type == Scanner.ScannerTypes.TCPIP)
                {
                    scanner = new ScannerTCPDataReceiver(new System.Net.IPEndPoint(IPAddress.Parse(configData.Scanner.Address), configData.Scanner.Port));
                    containerBulider.RegisterInstance(scanner).As<IisConnected>();
                }
                else
                {
                    scanner = new ScannerSerialDataReceiver(configData.Scanner.Address);
                    containerBulider.RegisterInstance(scanner).As<IisConnected>();
                    // containerBulider.RegisterType<ScannerTCPDataReceiverNull>().As<IisConnected>();
                }

                //containerBulider.RegisterType<ScannerTCPDataReceiverNull>().As<IisConnected>();

                ManualDataProvider manualDataProvider = new ManualDataProvider();

                List<IDataProvider> DataProviders = new List<IDataProvider>();
                DataProviders.Add(scanner);
                DataProviders.Add(manualDataProvider);

                containerBulider.RegisterType<Order>().AsSelf();
                containerBulider.RegisterType<Scan>().AsSelf();


                containerBulider.RegisterType<ScanDefinitionRepository>().As<IScanDefinitionRepository>();
                //containerBulider.RegisterType<SimpleScanDefinitionRepository>().As<IScanDefinitionRepository>();
                containerBulider.RegisterType<ScanTypeNameRepository>().As<IScanTypeNameRepository>();


                containerBulider.RegisterType<ScanExceptionRepository>().As<IScanExceptionRepository>();


                containerBulider.RegisterType<SQLUserRepository>().As<IUserRepository>();
                //containerBulider.Register(c => new SQLUserRepository(c.Resolve<ConfigData>().LocalConnectionString)).As<IUserRepository>();
                containerBulider.RegisterType<UserManagement>().AsSelf();


                containerBulider.RegisterInstance(_template).As<IEnumerable<ScanValuePart>>();
                containerBulider.RegisterType<StandardScanValueTemplate>().As<IScanValueTemplate>();


                containerBulider.RegisterType<ScanRecognizeService>().AsSelf();
                containerBulider.RegisterType<ManualDataProvider>().AsSelf();
                containerBulider.RegisterType<SQLOrderRepository>().As<IOrderRepository>();
                containerBulider.RegisterType<OrderFactory>().As<IOrderFactory>();
                containerBulider.RegisterType<SingleActivatorManager>().As<IActivatorManager>();

                containerBulider.RegisterType<MainViewModel>().SingleInstance();
                containerBulider.RegisterType<ScanDefinitionViewModel>().SingleInstance();
                containerBulider.RegisterType<ScanExceptionViewModel>().SingleInstance();
                containerBulider.RegisterType<LoginViewModel>().SingleInstance();

                containerBulider.RegisterInstance(configData).As<ConfigData>();
                containerBulider.RegisterInstance(manualDataProvider).As<ManualDataProvider>().SingleInstance();
                containerBulider.RegisterInstance(DataProviders).As<IEnumerable<IDataProvider>>();
                containerBulider.RegisterType<DataProviderComposite>().As<IDataProvider>();
                containerBulider.RegisterType<DataMapper>().As<IMapper<byte[], string>>();
                containerBulider.RegisterType<OrderCompletationService>().AsSelf();

                containerBulider.Register(c => new ShippingPrinter(configData.Printer.IP, configData.Printer.Port, configData.Printer.Template, c.Resolve<IScanValueTemplate>())).As<IPrintingService>();
                //containerBulider.Register(c => new ProcessingOrderEngine(c.Resolve<IDataProvider>(), c.Resolve<IMapper<byte[], string>>(), c.Resolve<IOrderFactory>(), c.Resolve<IOrderRepository>(), c.Resolve<OrderCompletationService>(), c.Resolve<IPrintingService>(), c.Resolve<ConfigData>().Settings.SilentMode, "Niezalogowano", c.Resolve<ScannerTCPDataReceiver>())).As<ProcessingOrderEngine>().SingleInstance();
                containerBulider.Register(c => new ProcessingOrderEngine(c.Resolve<IDataProvider>(), c.Resolve<IMapper<byte[], string>>(), c.Resolve<IOrderFactory>(), c.Resolve<IOrderRepository>(), c.Resolve<OrderCompletationService>(), c.Resolve<IPrintingService>(), c.Resolve<ConfigData>().Settings.SilentMode, "Niezalogowano")).As<ProcessingOrderEngine>().SingleInstance();

                container = containerBulider.Build();
            }

            public MainViewModel Main
            {
                get
                {
                    return container.Resolve<MainViewModel>();
                }
            }

            public ScanDefinitionViewModel ScanDefinitionVM
            {
                get
                {

                    return container.Resolve<ScanDefinitionViewModel>();
                }
            }

            public ScanExceptionViewModel ScanExceptionVM
            {
                get
                {
                    return container.Resolve<ScanExceptionViewModel>();
                }
            }


            public LoginViewModel LoginVM
            {
                get
                {
                    return container.Resolve<LoginViewModel>();
                }
            }

            public static void Cleanup()
            {
                // TODO Clear the ViewModels
            }




        }
    }