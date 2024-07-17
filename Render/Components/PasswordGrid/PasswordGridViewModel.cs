using System.Collections.Generic;
using System.Linq;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;

namespace Render.Components.PasswordGrid
{
    public class PasswordGridViewModel : ViewModelBase
    {
        [Reactive] public string Password { get; private set; }

        public bool CreatePassword { get; set; }

        private List<HexPoint> HexPoints { get; }

        [Reactive] public string ValidationMessage { get; private set; }

        [Reactive] public bool IncorrectPattern { get; private set; }

        public PasswordGridViewModel(IViewModelContextProvider viewModelContextProvider, string password)
            : base("PasswordGrid", viewModelContextProvider)
        {
            Password = password;
            CreatePassword = password.Length == 0;
            HexPoints = HexPoint.GetHexPoints();
        }

        public void AddToPassword(string hexToAdd)
        {
            if (!CreatePassword) return;
            var lastHex = Password.Length > 0 ? Password.Substring(Password.Length - 2, 2) : "";
            if (string.IsNullOrEmpty(lastHex))
            {
                Password = hexToAdd;
            }
            else
            {
                var lastPoint = HexPoints.First(x => x.HexValue == lastHex);
                if (lastPoint.HexValue == hexToAdd)
                    return;
                if (!lastPoint.IsNextPointValid(hexToAdd) || Password.Contains(hexToAdd))
                {
                    Password = hexToAdd;
                    return;
                }

                Password += hexToAdd;
            }
        }

        public void ResetPassword()
        {
            Password = "";
            CreatePassword = true;
        }

        public void SetValidation(string message)
        {
            ValidationMessage = message;
            IncorrectPattern = true;
        }

        public void ResetValidation()
        {
            ValidationMessage = "";
            IncorrectPattern = false;
        }
    }

    public class HexPoint
    {
        public static List<HexPoint> GetHexPoints()
        {
            var hexPoints = new List<HexPoint>();
            hexPoints.Add(new HexPoint("00", "01", "04", "05"));
            hexPoints.Add(new HexPoint("01", "00", "02", "03", "04", "05", "06"));
            hexPoints.Add(new HexPoint("02", "01", "03", "05", "06", "07"));
            hexPoints.Add(new HexPoint("03", "02", "06", "07"));
            hexPoints.Add(new HexPoint("04", "00", "01", "05", "08", "09"));
            hexPoints.Add(new HexPoint("05", "00", "01", "02", "04", "06", "08", "09", "0A"));
            hexPoints.Add(new HexPoint("06", "01", "02", "03", "05", "07", "09", "0A", "0B"));
            hexPoints.Add(new HexPoint("07", "02", "03", "06", "0A", "0B"));
            hexPoints.Add(new HexPoint("08", "04", "05", "09", "0C", "0D"));
            hexPoints.Add(new HexPoint("09", "04", "05", "06", "08", "0A", "0C", "0D", "0E"));
            hexPoints.Add(new HexPoint("0A", "05", "06", "07", "09", "0B", "0D", "0E", "0F"));
            hexPoints.Add(new HexPoint("0B", "06", "07", "0A", "0E", "0F"));
            hexPoints.Add(new HexPoint("0C", "08", "09", "0D"));
            hexPoints.Add(new HexPoint("0D", "08", "09", "0A", "0C", "0E"));
            hexPoints.Add(new HexPoint("0E", "09", "0A", "0B", "0D", "0F"));
            hexPoints.Add(new HexPoint("0F", "0A", "0B", "0E"));
            return hexPoints;
        }

        public string HexValue { get; }
        public List<string> ValidNextValues { get; }

        public HexPoint(string hexValue, params string[] args)
        {
            HexValue = hexValue;
            ValidNextValues = new List<string>(args);
        }

        public bool IsNextPointValid(string nextHexValue)
        {
            return ValidNextValues.Contains(nextHexValue);
        }
    }
}