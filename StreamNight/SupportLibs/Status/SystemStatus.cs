using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Status
{
    public class SystemStatus
    {
        /// <summary>
        /// Ordered dictionary of all tracked properties.
        /// </summary>
        public OrderedDictionary StatusProperties;

        /// <summary>
        /// Dictionary containing all tracked complex types.
        /// </summary>
        private Dictionary<string, Dictionary<string, object>> ComplexTypeTracker = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// Dictionary containing custom display names for tracked properties.
        /// </summary>
        public Dictionary<string, string> PropertyNameChanges = new Dictionary<string, string>
        {
            { "StreamRole", "StreamRoleName" },
            { "AdminRole", "AdminRoleName" },
            { "UseServerLogo", "UsingServerIcon" },
            { "Ready", "GuildReady" },
            { "Running", "BotRunning" },
            { "WebhookChannelMatch", "WebhookAndChannelMatch" }
        };

        /// <summary>
        /// String array containing the names of all tracked DiscordClient properties.
        /// </summary>
        public string[] MonitoredProperties = new string[] 
        {
            "StreamUp",
            "TwitchEnabled",
            "StreamChannelName",
            "StreamRole",
            "AdminRole",
            "UseServerLogo",
            "Ready",
            "Running",
            "WebhookChannelMatch",
            "CurrentPresence",
            "RedirectPlaylist",
            "RedirectTarget"
        };

        /// <summary>
        /// The timer used to periodically check the client properties.
        /// </summary>
        private static System.Timers.Timer StatusUpdateTimer;

        /// <summary>
        /// The Discord client this status object is tracking.
        /// </summary>
        private readonly Discord.Client discordClient;

        /// <summary>
        /// The SignalR hub used to send notifications to connected clients.
        /// </summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public readonly IHubContext<StatusHub> statusHub;

        public SystemStatus(DiscordBot discordBot, IHubContext<StatusHub> statusHubContext)
        {
            statusHub = statusHubContext;

            discordClient = discordBot.DiscordClient;

            StatusUpdateTimer = new System.Timers.Timer(5000);
            StatusUpdateTimer.Elapsed += UpdateStatusFromEvent;
            StatusUpdateTimer.AutoReset = true;
            StatusUpdateTimer.Enabled = true;

            discordClient.SystemStatus = this;

            InitialiseDictionary();
        }

        /// <summary>
        /// Creates new dictionaries and populates them by walking through all properties up to one child deep.
        /// </summary>
        private void InitialiseDictionary()
        {
            StatusProperties = new OrderedDictionary
            {
                { "LastPoll", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "LastChange", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            };

            PropertyInfo currentProperty;
            foreach (string property in MonitoredProperties)
            {
                currentProperty = discordClient.GetType().GetProperty(property);

                if (currentProperty.PropertyType.Module.ScopeName != "CommonLanguageRuntimeLibrary"
                    && currentProperty.PropertyType.Module.ScopeName != "System.Private.CoreLib.dll")
                {
                    // The property is a complex type, completely unwrap it and add its items to the tracking dictionary
                    object propertyInfo = currentProperty.GetValue(discordClient);

                    if (ComplexTypeTracker.ContainsKey(currentProperty.Name))
                    {
                        throw new InvalidOperationException("Can't add properties with identical names to the tracking dictionary.");
                    }
                    else
                    {
                        Dictionary<string, object> ComplexTypeProperties = UnwrapComplexType(propertyInfo);
                        foreach (KeyValuePair<string, object> complexTypeProperty in ComplexTypeProperties)
                        {
                            StatusProperties.Add(complexTypeProperty.Key, complexTypeProperty.Value);
                        }

                        ComplexTypeTracker.Add(currentProperty.Name, ComplexTypeProperties);
                        // Skip the rest of the iteration, maybe use an else clause instead?
                        continue;
                    }
                }

                // Adding all of the properties to the OrderedDictionary to let us
                // simply update the existing one instead of recreating it each time
                if (PropertyNameChanges.ContainsKey(property))
                {
                    StatusProperties.Add(PropertyNameChanges[property], currentProperty.GetValue(discordClient));
                }
                else
                {
                    StatusProperties.Add(property, currentProperty.GetValue(discordClient));
                }
            }
        }

        /// <summary>
        /// Requests status update. Called from timer.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Elapsed event arguments.</param>
        private void UpdateStatusFromEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.UpdateStatus();
        }

        /// <summary>
        /// Event used to signal a change in system status.
        /// </summary>
        public event EventHandler StatusChanged;
        /// <summary>
        /// Boolean used to track if an event should be raised.
        /// </summary>
        private bool RaiseStatusChanged;

        /// <summary>
        /// Updates the current status object and raises event if required.
        /// </summary>
        public void UpdateStatus()
        {
            PropertyInfo currentProperty;
            foreach (string property in MonitoredProperties)
            {
                bool objectIsComplexType = false;
                currentProperty = discordClient.GetType().GetProperty(property);

                object propertyInfo = currentProperty.GetValue(discordClient);

                if (currentProperty.PropertyType.Module.ScopeName != "CommonLanguageRuntimeLibrary"
                    && currentProperty.PropertyType.Module.ScopeName != "System.Private.CoreLib.dll")
                {
                    // The property is a complex type, completely unwrap it and check the resulting dictionary against the stored one
                    if (ComplexTypeTracker.ContainsKey(currentProperty.Name))
                    {
                        objectIsComplexType = true;
                        Dictionary<string, object> ComplexTypeProperties = UnwrapComplexType(propertyInfo);

                        // If the number of items is not equal (shouldn't happen with static typing)
                        // or the properties in each are unequal.
                        // https://stackoverflow.com/a/3804852/
                        if (ComplexTypeTracker[currentProperty.Name].Count != ComplexTypeProperties.Count
                            || ComplexTypeTracker[currentProperty.Name].Except(ComplexTypeProperties).Any())
                        {
                            foreach (KeyValuePair<string, object> complexTypeProperty in ComplexTypeProperties)
                            {
                                StatusProperties[complexTypeProperty.Key] = complexTypeProperty.Value;
                            }
                            this.RaiseStatusChanged = true;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Complex type does not exist in the tracking list.");
                    }
                }

                // Using Equals() because .NET would otherwise treat them as reference types and always show unequal
                // This is probably because they're marked as "Object" and not as "string" or "long".
                if (!objectIsComplexType && PropertyNameChanges.ContainsKey(property) && !Equals(StatusProperties[PropertyNameChanges[property]], propertyInfo))
                {
                    // Property has changed and has custom display name
                    StatusProperties[PropertyNameChanges[property]] = propertyInfo;
                    this.RaiseStatusChanged = true;
                }
                else if (!objectIsComplexType && !PropertyNameChanges.ContainsKey(property) && !Equals(StatusProperties[property], propertyInfo))
                {
                    // Property has changed and is using default name
                    StatusProperties[property] = propertyInfo;
                    this.RaiseStatusChanged = true;
                }
            }

            StatusProperties["LastPoll"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (this.RaiseStatusChanged)
            {
                StatusProperties["LastChange"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                StatusChanged.Invoke(this, new EventArgs());
                this.RaiseStatusChanged = false;
            }
        }

        // https://stackoverflow.com/a/14684048/
        /// <summary>
        /// Unwraps a complex type into their individual components. Appends parent names to objects more than one layer deep.
        /// </summary>
        /// <param name="obj">The object to unwrap.</param>
        /// <param name="parent">The object's parent, only used if recursively called.</param>
        /// <param name="workingDict">The dictionary to populate the children into, only used if recursively called.</param>
        /// <returns></returns>
        private Dictionary<string, object> UnwrapComplexType(object obj, string parent = null, Dictionary<string, object> workingDict = null)
        {
            // Checks if a dictionary doesn't exist and creates a new one if applicable
            // Uses the specified argument if not null
            if (workingDict == null)
            {
                workingDict = new Dictionary<string, object>();
            }

            Type objectType = obj.GetType();
            PropertyInfo[] objectProperties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo propertyInfo in objectProperties)
            {
                // Basic check to handle both .NET Framework and .NET Core base class types
                // Unwraps child complex properties
                if (propertyInfo.PropertyType.Module.ScopeName != "CommonLanguageRuntimeLibrary"
                    && propertyInfo.PropertyType.Module.ScopeName != "System.Private.CoreLib.dll")
                {
                    string objectTypeName;
                    if (parent != null)
                    {
                        objectTypeName = $"{parent}.{objectType.Name}";
                    }
                    else
                    {
                        objectTypeName = objectType.Name;
                    }

                    // Attempts to unwrap the property into a dictionary
                    Dictionary<string, object> childProperties = UnwrapComplexType(propertyInfo.GetValue(obj), objectTypeName);

                    // Check if the property has any children of its own (complex type)
                    if (childProperties.Count != 0)
                    {
                        // Removes duplicate keys and concatencates the dictionaries
                        // All child properties should already be completely unwrapped through the recursive call above
                        workingDict.Concat(childProperties).GroupBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);
                    }
                    // Property is already its most basic type, add it to the working dictionary
                    else
                    {
                        workingDict.Add(propertyInfo.Name, propertyInfo.GetValue(obj).ToString());
                    }
                }
                // Property is provided by the base class, shouldn't contain any child types
                else
                {
                    object value = propertyInfo.GetValue(obj);
                    workingDict.Add(propertyInfo.Name, value);
                }
            }

            // Return the entire working dictionary
            return workingDict;
        }
    }
}
