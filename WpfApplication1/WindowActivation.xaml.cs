using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApplication1.Properties;
using Microsoft.Win32;
using WpfApplication1.ServiceRefServARCode;
using System.ServiceModel;
using SimpleSecurity;
using WpfApplication1.DTO;
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

        public WindowAc()
        {
            InitializeComponent();
            SetUserData(currentUser);
            this.Focus();
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
            if (!String.IsNullOrEmpty(aC) && aC.Equals(textBox_ACode.Text))
            {
                Window1.isNotRegistred = false;
                using (RegistryKey currentUserKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true))
                {
                    RegistryKey sskaKey = currentUserKey.OpenSubKey("sskvz", true);
                    if (sskaKey != null)
                    {
                        sskaKey.SetValue("isT", false);
                        sskaKey.SetValue("ed", DateTime.MaxValue.Date.ToString("d"));
                        sskaKey.Close();
                    }
                    this.txtBlock_Status.Text = "Thank you for the succeded activation.\nActivation dialog may be closed forever.";
                }
            }
            else
            {
                this.txtBlock_Status.Text =
                    String.Format("Activation failed. Trial perriod: 60 days.\r\nPlease send your Activation Request Code\r\nfrom E-Mail: {0}  to vladzdravkov@gmail.com.\r\nSoon after payment You receive the activation code.",
                    txtBox_UEmail.Text);
            }
        }

        private void button_GetActCode_Click(object sender, RoutedEventArgs e)
        {
            string recievedACode = GetActivationCode();     
            textBox_ACode.Text = String.IsNullOrEmpty(recievedACode) ? "Activation failed. Please, try again later." :  recievedACode;
        }

        private void button_SendAcReq_Click(object sender, RoutedEventArgs e)
        {
            using (ServiceRefServARCode.ServARCodeClient serviceClient = new ServARCodeClient())
            {
                AuthorizationRequest request = new AuthorizationRequest()
                {
                    AccountId = Settings.Default.AccId,
                    AuthCode = aC,
                    AuthRequestCode = Settings.Default.ActivationRequestCode,
                    UserEmail = Settings.Default.UserEmail,
                    UserName = Settings.Default.UserName,
                    UserFirstName = Settings.Default.UserFirstName
                };
                try
                {
                    int resultedAccId = serviceClient.TryToRegisterAuthRequest(request);
                    if (resultedAccId > 0)
                    {
                        this.txtBlock_Status.Text = "Activation request succeded. \r\nActivation Code will be sent after the payment.";
                        button_ResetRequest.IsEnabled = true;
                        button_SendAcReq.IsEnabled = false;
                        btn_GetARC.IsEnabled = false;
                        txtBlock_ARC.IsReadOnly = true;
                        txtBox_UEmail.IsReadOnly = true;
                        textBox_Name.IsReadOnly = true;
                        textBox_FirstName.IsReadOnly = true;
                        Settings.Default.IsActivationReqSent = true;
                        Settings.Default.AccId = resultedAccId;
                        Settings.Default.Save();
                    }
                    else this.txtBlock_Status.Text = String.Format("Activation request failed. \r\n Please send on E-mail {0} data from this form. \r\n Activation Code will be sent on your E-Mail after the payment.", Settings.Default.OwnerEmail);
                }
                catch (TimeoutException ex)
                {
                    txtBlock_Status.Text = "Timeout: " + ex.InnerException;
                }
                catch (FaultException<AuthorizationRequestFault> ex)
                {
                    txtBlock_Status.Text = "Remote service Error: " + ex.Detail.FaultMessage;
                }
                catch (CommunicationException ex)
                {
                    txtBlock_Status.Text = "Remote communication Error: " + ex.Message;
                }
                catch (Exception ex)
                {
                    txtBlock_Status.Text = "Unknown remote service exception: " + ex.Message;
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
            txtBlock_Status.Text = "Activation Request Code is generated. \r\nPlease, submit Activation Request.";
            if (!txtBlock_ARC.IsFocused)
                txtBlock_ARC.Focus();
            currentUser.ActivationRequestCode = aRC;
            button_GetActCode.Focus();

            e.Handled = true;
        }

        private String GetActivationCode()
        {
            string recievedACode = String.Empty;
            using (ServiceRefServARCode.ServARCodeClient serviceClient = new ServARCodeClient())
            {
                try
                {
                    recievedACode = serviceClient.GetAuthCode(Settings.Default.AccId, Settings.Default.ActivationRequestCode);
                    txtBlock_Status.Text = "Activation Code received.\n Activation is unlocked.";
                }
                catch (TimeoutException ex)
                {
                    txtBlock_Status.Text = "Timeout: " + ex.InnerException;
                }
                catch (FaultException<AuthorizationRequestFault> ex)
                {
#if DEBUG
                    txtBlock_Status.Text = "Remote service Error: \r\n" + ex.Detail.FaultMessage;
#else
                    txtBlock_Status.Text = String.Format("Remote service failed. \r\nYou can communicate with the owner via \r\n{0}.", Settings.Default.OwnerEmail);
#endif
                }
                catch (CommunicationException ex)
                {
                    txtBlock_Status.Text = "Remote communication Error: " + ex.Message;
                }
                catch (Exception ex)
                {
                    txtBlock_Status.Text = "Unknown remote service exception: " + ex.Message;
                }
            }            
            return recievedACode;
        }
    }
}
