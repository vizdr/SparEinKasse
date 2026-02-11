using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApplication1.Properties;
using SimpleSecurity;
using WpfApplication1.DTO;
using WpfApplication1.Services;
using System.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for WindowAc.xaml
    /// </summary>
    public partial class WindowAc : Window
    {
        static Mutex mutexObjActWin = new Mutex();
        User currentUser = new User();
        private int _errors = 0;
        private static string aRC = Settings.Default.ActivationRequestCode;
        private static string aC;
        private static string userEmail = Settings.Default.UserEmail;
        private readonly RegistrationManager _registration;

        public WindowAc(RegistrationManager registration)
        {
            _registration = registration ?? throw new ArgumentNullException(nameof(registration));
            InitializeComponent();
            ApplyLocalization();
            SetUserData(currentUser);
            this.Focus();
        }

        private void ApplyLocalization()
        {
            var loc = RuntimeLocalization.Instance;
            Title = loc["ActTitle"];
            label2.Content = loc["ActEnterFirstName"];
            labelName.Content = loc["ActEnterName"];
            lbl_UName.Content = loc["ActEnterEmail"];
            label1.Content = loc["ActRequestCode"];
            btn_GetARC.Content = loc["ActGenerate"];
            button_SendAcReq.Content = loc["ActSubmitRequest"];
            button_ResetRequest.Content = loc["ActResetRequest"];
            btn_Ac.Content = loc["ActActivate"];
            button_GetActCode.Content = loc["ActGetCode"];
            textBox_ACode.Text = loc["ActEnterCodeHint"];
            buttonClose.Content = loc["ActClose"];
        }
       
        private void SetUserData(User user)
        {
            if (Settings.Default.IsActivationReqSent)
            {
                String activationCode = GetActivationCode();
                user.ActivationRequestCode = Settings.Default.ActivationRequestCode;
                user.UserEmail = Settings.Default.UserEmail;
                user.FirstName = Settings.Default.UserFirstName;
                user.Name = Settings.Default.UserName;
                aC = Security.GetAuthorizationCode(Settings.Default.ActivationRequestCode);
                txtBlock_ARC.Text = aC;
                btn_GetARC.IsEnabled = false;
                button_SendAcReq.IsEnabled = false;
                button_ResetRequest.IsEnabled = true;
                txtBlock_ARC.IsReadOnly = true;
                txtBox_UEmail.IsReadOnly = true;
                textBox_FirstName.IsReadOnly = true;
                textBox_Name.IsReadOnly = true;
                if (!String.IsNullOrEmpty(activationCode))
                {
                    textBox_ACode.Text = activationCode;
                    button_GetActCode.IsEnabled = false;
                }
            }
            else
            {
                button_ResetRequest.IsEnabled = false;
                btn_GetARC.IsEnabled = true;
                button_SendAcReq.IsEnabled = true;
                button_GetActCode.IsEnabled = true;                                
            }
            this.DataContext = user;
        }
        private void btn_Activate_Click(object sender, RoutedEventArgs e)
        {
            var loc = RuntimeLocalization.Instance;
            if (!String.IsNullOrEmpty(aC) && aC.Equals(textBox_ACode.Text))
            {
                _registration.MarkAsRegistered();
                this.txtBlock_Status.Text = loc["ActSucceeded"];
            }
            else
            {
                this.txtBlock_Status.Text = String.Format(loc["ActFailed"],
                    txtBox_UEmail.Text, Settings.Default.OwnerEmail);
            }
        }

        private void button_GetActCode_Click(object sender, RoutedEventArgs e)
        {
            string recievedACode = GetActivationCode();
            textBox_ACode.Text = String.IsNullOrEmpty(recievedACode) ? RuntimeLocalization.Instance["ActGetCodeFailed"] : recievedACode;
        }

        private void button_SendAcReq_Click(object sender, RoutedEventArgs e)
        {
            using (var authService = new FallbackAuthorizationService())
            {
                var request = new AuthorizationRequestData
                {
                    AccountId = Settings.Default.AccId,
                    AuthCode = "0",
                    AuthRequestCode = Settings.Default.ActivationRequestCode,
                    UserEmail = Settings.Default.UserEmail,
                    UserName = Settings.Default.UserName,
                    UserFirstName = Settings.Default.UserFirstName
                };

                var result = authService.TryToRegisterAuthRequest(request);

                var loc = RuntimeLocalization.Instance;
                if (result.Success && result.AccountId > 0)
                {
                    this.txtBlock_Status.Text = String.Format(loc["ActRequestSucceeded"], result.ServiceUsed);
                    button_ResetRequest.IsEnabled = true;
                    button_SendAcReq.IsEnabled = false;
                    btn_GetARC.IsEnabled = false;
                    txtBlock_ARC.IsReadOnly = true;
                    txtBox_UEmail.IsReadOnly = true;
                    textBox_Name.IsReadOnly = true;
                    textBox_FirstName.IsReadOnly = true;
                    Settings.Default.IsActivationReqSent = true;
                    Settings.Default.AccId = result.AccountId;
                    Settings.Default.Save();
                }
                else
                {
                    this.txtBlock_Status.Text = String.Format(loc["ActRequestFailed"],
                        result.ServiceUsed ?? "Unknown",
                        result.Message,
                        Settings.Default.OwnerEmail);
                }
            }
        }

        private void button_ResetRequest_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.IsActivationReqSent = false;
            btn_GetARC.IsEnabled = true;
            button_SendAcReq.IsEnabled = true;
            txtBlock_ARC.Text = String.Empty;
            txtBox_UEmail.IsReadOnly = false;
            textBox_FirstName.IsReadOnly = false;
            textBox_Name.IsReadOnly = false;
            txtBlock_Status.Text = string.Empty;
            Settings.Default.Save();
        }

        private void Validation_Error(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
                _errors++;
            else
                _errors--;
        }

        private void GetARCode_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _errors == 0;
            e.Handled = true;
        }

        private void GetARCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            aRC = Security.GetRequestCode(txtBox_UEmail.Text);
            txtBlock_ARC.Text = aRC;

            aC = Security.GetAuthorizationCode(aRC);

            Settings.Default.ActivationRequestCode = aRC;
            Settings.Default.UserEmail = txtBox_UEmail.Text;
            Settings.Default.UserFirstName = textBox_FirstName.Text;
            Settings.Default.UserName = textBox_Name.Text;
            Settings.Default.Save();
            txtBlock_Status.Text = RuntimeLocalization.Instance["ActCodeGenerated"];
            if (!txtBlock_ARC.IsFocused)
                txtBlock_ARC.Focus();
            currentUser.ActivationRequestCode = aRC;
            button_GetActCode.Focus();

            e.Handled = true;
        }

        private String GetActivationCode()
        {
            string recievedACode = String.Empty;
            using (var authService = new FallbackAuthorizationService())
            {
                var result = authService.GetAuthCode(Settings.Default.AccId, Settings.Default.ActivationRequestCode);

                var loc = RuntimeLocalization.Instance;
                if (result.Success && !string.IsNullOrEmpty(result.AuthCode))
                {
                    recievedACode = result.AuthCode;
                    txtBlock_Status.Text = String.Format(loc["ActCodeReceived"], result.ServiceUsed);
                }
                else
                {
#if DEBUG
                    txtBlock_Status.Text = String.Format(loc["ActRemoteServiceError"], result.ServiceUsed, result.Message);
#else
                    txtBlock_Status.Text = String.Format(loc["ActRemoteServiceFailed"], Settings.Default.OwnerEmail);
#endif
                }
            }
            return recievedACode;
        }
    }
}
