using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using EnterpriseDT.Net.Ftp;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using JdSoft.Apple.Apns.Notifications;
using System.Diagnostics;
using System.Configuration;

   public class PushToApple
    {
       // public void pushNotification(List<NotificationList> notificationlist)
        public void pushNotification()
        {
            //True if you are using sandbox certificate, or false if using production
            bool sandbox = bool.Parse(ConfigurationManager.AppSettings["use_sandbox"]);

            //Put your PKCS12 .p12 or .pfx filename here.
            // Assumes it is in the same directory as your app
            //string p12File = "apn_developer_identity.p12";
            string p12File = ConfigurationManager.AppSettings["ssl_p12_file"];

            //This is the password that you protected your p12File 
            //  If you did not use a password, set it as null or an empty string
            string p12FilePassword = ConfigurationManager.AppSettings["ssl_p12_password"];

            //Number of milliseconds to wait in between sending notifications in the loop
            // This is just to demonstrate that the APNS connection stays alive between messages
            int sleepBetweenNotifications = Int32.Parse(ConfigurationManager.AppSettings["sleep_between_notifications"]);

            //Actual Code starts below:
            //--------------------------------
            string p12Filename = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p12File);

            //Create our notification service object
            NotificationService service = new NotificationService(sandbox, p12Filename, p12FilePassword, 1);

            //Set some custom info from our config
            service.SendRetries = Int32.Parse(ConfigurationManager.AppSettings["send_retries"]);
            service.ReconnectDelay = Int32.Parse(ConfigurationManager.AppSettings["reconnect_delay"]);

            //Assign handlers
            service.Error += new NotificationService.OnError(service_Error);
            service.NotificationTooLong += new NotificationService.OnNotificationTooLong(service_NotificationTooLong);
            service.BadDeviceToken += new NotificationService.OnBadDeviceToken(service_BadDeviceToken);
            service.NotificationFailed += new NotificationService.OnNotificationFailed(service_NotificationFailed);
            service.NotificationSuccess += new NotificationService.OnNotificationSuccess(service_NotificationSuccess);
            service.Connecting += new NotificationService.OnConnecting(service_Connecting);
            service.Connected += new NotificationService.OnConnected(service_Connected);
            service.Disconnected += new NotificationService.OnDisconnected(service_Disconnected);

            //Loop through our results and send our notifications
            Int32 reminderCount = 0;

           // foreach (var notification in notificationlist)
            //{
            //7dc011b51c4083a6ff79c5d86fb4007bf02ed9c4d43499a5ab19e69743d13a6a   iphone - live
            //9c12fe4627e2f03c23893f78b17a275501b3dd09826f8fa285331301ec53a6ac   iphone - qa
            //947f18419bfdf9be9540f1465724d16358b9ff5e8ecff9ef9e7c8d082e8a13e4   ipod - qa
            //d54f9016b8e4eb32b99f9fc3eddcb590e95b8f010a21de308323bfdbff92bba9   ipod - live
            string deviceToken = "435af7041528e5caaa4fe6f1d72f8cd78be4112b0818626db453b15eb2a1c7eb"; //notification.PhoneID;

                //has to be the correct length for a device token, or badness occurs
                if (deviceToken.Length == 64)
                {
                    //Create a new notification to send to all devices with our app device token
                    Notification alertNotification = new Notification(deviceToken);

                    //create our message body. 
                    string forecastMessage = "PRAVEEN TESTING PUSH NOTIFICATION FOR VPS";
                    //(877) 378-9517 has a New Fax from (800) 931-7145 on 7/11/2011 6:49 AM
                    //(877) 378-9517 has a New Message from (800) 278-6103 on 7/11/2011 3:01 AM
                    //(877) 378-9517 EXT 755 has a New Message from (661) 380-4363 on 7/11/2011 12:22 AM

                    //Create our payload
                    alertNotification.Payload.Alert.Body = forecastMessage;
                    alertNotification.Payload.Sound = "default";
                    alertNotification.Payload.Badge = 5; 
                    //alertNotification.Payload.AddCustom("CustomVariable","123456");
                    
                    //Queue the notification to be sent);
                    if (service.QueueNotification(alertNotification))
                        writeToLogFile("Notification Queued!", EventLogEntryType.Information);
                    else
                        writeToLogFile("Notification Failed to be Queued!", EventLogEntryType.Error);

                    //Sleep in between each message
                  //  if (reminderCount < notificationlist.Count)
                   // {
                   // writeToLogFile("Sleeping " + sleepBetweenNotifications + " milliseconds before next Notification...", EventLogEntryType.Information);
                   // System.Threading.Thread.Sleep(sleepBetweenNotifications);
                   // }
                }
                else
                {
                    writeToLogFile("Invalid device token length, possible simulator entry: " + deviceToken, EventLogEntryType.Information);
                }
                reminderCount++;
           // }

            writeToLogFile("Cleaning Up...", EventLogEntryType.Information);

            //First, close the service (This ensures any queued notifications get sent before the connections are closed).
            service.Close();

            //Clean up
            service.Dispose();

            //Report success
            writeToLogFile("Done!", EventLogEntryType.Information);
        }

        public static void writeToLogFile(string logMessage, EventLogEntryType type)
        {
            
            //create log files by datetime to avoid one massive log file :-)
            string strLogFileName = ConfigurationManager.AppSettings["logFilePath"] + "log" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + ".txt";
            string strLogMessage = string.Format("{0} [{1}]: {2}" + Environment.NewLine, DateTime.Now, type.ToString(), logMessage);
            //File.AppendAllText(strLogFileName, strLogMessage);

        }

        static void service_BadDeviceToken(object sender, BadDeviceTokenException ex)
        {
            writeToLogFile(string.Format("Bad Device Token: {0}", ex.Message), EventLogEntryType.Error);
        }

        static void service_Disconnected(object sender)
        {
            writeToLogFile("Disconnected...", EventLogEntryType.Information);
        }

        static void service_Connected(object sender)
        {
            writeToLogFile("Connected...", EventLogEntryType.Information);
        }

        static void service_Connecting(object sender)
        {
            writeToLogFile("Connecting...", EventLogEntryType.Information);
        }

        static void service_NotificationTooLong(object sender, NotificationLengthException ex)
        {
            writeToLogFile(string.Format("Notification Too Long: {0}", ex.Notification.ToString()), EventLogEntryType.Error);
        }

        static void service_NotificationSuccess(object sender, Notification notification)
        {
            writeToLogFile(string.Format("Notification Success: {0}", notification.ToString()), EventLogEntryType.Information);
        }

        static void service_NotificationFailed(object sender, Notification notification)
        {
            writeToLogFile(string.Format("Notification Failed: {0}", notification.ToString()), EventLogEntryType.Error);
        }

        static void service_Error(object sender, Exception ex)
        {
            writeToLogFile(string.Format("Error: {0}", ex.Message), EventLogEntryType.Information);
        }
    }
