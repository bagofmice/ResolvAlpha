using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.UI.Notifications;

namespace Resolv
{

    public static class ResolvFenceHelper
    {
        public static IList<Geofence> GetGeofences()
        {
            return GeofenceMonitor.Current.Geofences;
        }

        public static IList<Geofence> GetResolvGeofences()
        {
            List<Geofence> resolvGeofences = new List<Geofence>();
            foreach (Geofence g in GeofenceMonitor.Current.Geofences)
            {
                if (g.Id.StartsWith("resolv"))
                {
                    resolvGeofences.Add(g);
                }
            }

            return resolvGeofences;
        }

        public static void CreateGeofence(string id, double lat, double lon, double radius, TimeSpan dwelltime)
        {
            if (GeofenceMonitor.Current.Geofences.SingleOrDefault(g => g.Id == id) != null) return;

            BasicGeoposition position = new BasicGeoposition();
            position.Latitude = lat;
            position.Longitude = lon;

            Geocircle geocircle = new Geocircle(position, radius);

            MonitoredGeofenceStates mask = 0;
            mask |= MonitoredGeofenceStates.Entered;

            // Create Geofence with the supplied id, geocircle and mask, not for single use
            // and with a dwell time of 5 seconds
            Geofence geofence = new Geofence(id, geocircle, mask, false, dwelltime);
            GeofenceMonitor.Current.Geofences.Add(geofence);
        }

        public static void RemoveGeofence(string id)
        {
            var geofence = GeofenceMonitor.Current.Geofences.SingleOrDefault(g => g.Id == id);

            if (geofence != null)
                GeofenceMonitor.Current.Geofences.Remove(geofence);
        }
    }

    class ResolvGeoFenceTask : IBackgroundTask
    {

        // Task Standard Registration
        const string ResolvTaskName = "ResolvFenceTask";
        /// <summary>
        /// Don't re-register
        /// </summary>
        /// <returns>If it's registered.</returns>
        public static bool ISResolvBackTaskRegistered()
        {
            
            var resolvTask =
                BackgroundTaskRegistration.AllTasks.FirstOrDefault(kvp => kvp.Value.Name.Equals(ResolvTaskName));

            if (resolvTask.Value != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async static void RegisterResolvBackgroundTask()
        {
            if (!ISResolvBackTaskRegistered())
            {
                object waitFor = await BackgroundExecutionManager.RequestAccessAsync();
                BackgroundTaskBuilder resolvBuilder = new BackgroundTaskBuilder();
                resolvBuilder.Name = ResolvTaskName;
                resolvBuilder.TaskEntryPoint = typeof (ResolvGeoFenceTask).FullName;
                resolvBuilder.SetTrigger(new LocationTrigger(LocationTriggerType.Geofence));
                resolvBuilder.Register();
            }
        }

        /// <summary>
        /// Where the action is.
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get the information of the geofence(s) that have been hit
            var reports = GeofenceMonitor.Current.ReadReports();
            var report = reports.FirstOrDefault(r => (r.Geofence.Id == "MyGeofenceId") && (r.NewState == GeofenceState.Entered));

            if (report == null) return;

            // Create a toast notification to show a geofence has been hit
            var toastXmlContent = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            var txtNodes = toastXmlContent.GetElementsByTagName("text");
            txtNodes[0].AppendChild(toastXmlContent.CreateTextNode("Geofence triggered toast!"));
            txtNodes[1].AppendChild(toastXmlContent.CreateTextNode(report.Geofence.Id));

            var toast = new ToastNotification(toastXmlContent);
            var toastNotifier = ToastNotificationManager.CreateToastNotifier();
            toastNotifier.Show(toast);
        }


    }
}
