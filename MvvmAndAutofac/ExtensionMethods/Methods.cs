using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityScansReplacement.Domain.Model;
using TraceabilityScansReplacement.UI.Model;
using TraceabilityScansReplacement.UI.ViewModel;

namespace MvvmAndAutofac.ExtensionMethods
{
    public static class Methods
    {
        public static List<ScanPropertyViewModel> ListScanPropertyToModelView(List<ScanProperty> input)
        {
            List<ScanPropertyViewModel> list = new List<ScanPropertyViewModel>();

            foreach (ScanProperty s in input)
            {
                list.Add(new ScanPropertyViewModel(s));
            }

            return list;
        }
        public static List<ScanProperty> ListModelViewToScanProperty(List<ScanPropertyViewModel> input)
        {
            List<ScanProperty> list = new List<ScanProperty>();

            foreach (ScanPropertyViewModel s in input)
            {
                list.Add(new ScanProperty() { Id = s.Id, Color = s.Color, Value = s.Value });
            }

            return list;
        }

        public static String CollectionToString(this ObservableCollection<StringValue> input)
        {
            String value = "";

            foreach (var s in input)
            {
                value += s.Value;
            }

            return value;
        }

        public static ObservableCollection<CharValue> StringToCharCollection(this string input, int lenght)
        {
            ObservableCollection<CharValue> list = new ObservableCollection<CharValue>();


            int count = input.Count();
            for (int i = 0; i < lenght; i++)
            {

                if (i < count && input[i] != '_')
                {
                    list.Add(new CharValue(input[i], i + 1));
                }
                else
                {
                    list.Add(new CharValue(null, i + 1));
                }
            }

            return list;
        }

        public static String CollectionCharToString(this ObservableCollection<CharValue> input)
        {
            String value = "";

            foreach (var s in input)
            {
                if (string.IsNullOrWhiteSpace(s.Value.ToString()))
                {
                    value += '_';
                }
                else
                {
                    value += s.Value;
                }
            }
            value = value.TrimEnd('_');
            return value;
        }

        public static int BitArrayToInt(this ObservableCollection<Boolean> input)
        {
            int value = 0;

            for (int i = 0; i < input.Count; i++)
            {
                if (input[i])
                    value += Convert.ToInt16(Math.Pow(2, i));
            }

            return value;
        }

        public static bool[] IntToBitArray(this int input)
        {
            //if (input == 0) return new[] { false };
            var n = (int)(Math.Log(input) / Math.Log(2));
            //n = 8;
            var a = new bool[8];
            for (var i = n; i >= 0; i--)
            {
                n = (int)Math.Pow(2, i);
                if (n > input) continue;
                a[7-i] = true;
                input -= n;
            }
            Array.Reverse(a);
            return a;
        }
    }
}
