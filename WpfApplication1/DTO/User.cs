using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace WpfApplication1.DTO
{
    public class User : IDataErrorInfo, INotifyPropertyChanged
    {
        private const string emailPattern = @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$";
        private string name;
        private string firstName;
        private string userEmail;
        private string activationRequestCode;


        [Required(ErrorMessage = "Name must be provided")]
        [RegularExpression(@"^[A-Za-z0-9_-]{3,30}$", ErrorMessage = "Name is not valid")]
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        [Required(ErrorMessage = "First Name must be provided")]
        [RegularExpression(@"^[A-Za-z0-9_-]{3,30}$", ErrorMessage = "First Name is not valid")]
        public string FirstName
        {
            get { return firstName; }
            set
            {
                firstName = value;
                OnPropertyChanged("FirstName");
            }
        }

        [Required(ErrorMessage = "E-mail must be provided")]
        [RegularExpression(emailPattern, ErrorMessage = "E-Mail is not valid")]
        public string UserEmail
        {
            get { return userEmail; }
            set
            {
                userEmail = value;
                OnPropertyChanged("UserEmail");
            }
        }
        [DataType(DataType.Text)]
        [RegularExpression(@"^[A-Za-z0-9_-]{1,20}$", ErrorMessage = "ActivationRequestCode is not valid")]
        public string ActivationRequestCode
        {
            get { return activationRequestCode; }
            set
            {
                activationRequestCode = value;
                OnPropertyChanged("AuthorizationCode");
            }
        }

        #region IDataErrorInfo Members

        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        public string this[string propertyName]
        {

            get
            {
                if (propertyName == "Name")
                    return ValidateProperty(this.Name, propertyName);
                if (propertyName == "FirstName")
                    return ValidateProperty(this.FirstName, propertyName);
                if (propertyName == "UserEmail")
                    return ValidateProperty(this.UserEmail, propertyName);
                if (propertyName == "ActivationRequestCode")
                    return ValidateProperty(this.ActivationRequestCode, propertyName);
                return null;
            }

        }

        #endregion

        protected string ValidateProperty(object value, string propertyName)
        {
            var info = this.GetType().GetProperty(propertyName);
            IEnumerable<string> errorInfos =
                  (from va in info.GetCustomAttributes(true).OfType<ValidationAttribute>()
                   where !va.IsValid(value)
                   select va.FormatErrorMessage(string.Empty)).ToList();

            if (errorInfos.Count() > 0)
            {
                return errorInfos.FirstOrDefault<string>();
            }
            return null;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
    }
}
