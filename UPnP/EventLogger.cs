/*   
Copyright 2006 - 2010 Intel Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace OpenSource.Utilities
{

	/// <summary>
	/// This class can be used as the master event logging class for all
	/// of an application and its libraries. The application should set the
	/// system log file upon startup. All exceptions should be sent to the log
	/// using the Log(Exception) method.
	/// </summary>
	public sealed class EventLogger
	{
		public delegate void EventHandler(EventLogEntryType LogType, object origin, string StackTrace, string LogMessage);
		public static event EventHandler OnEvent;
		public static bool Enabled = false;
		public static bool ShowAll = false;

	    private static bool   g_onExceptionShowMessage;
		private static EventLog log;

		/// <summary>
		/// Set the application wide event logging to a Windows event log file.
		/// </summary>
		/// <param name="sourceName">Generaly the name of the application</param>
		/// <param name="logName">The name of the system log file</param>
		/// <param name="productVersion">The application's version string</param>
		public static void SetLog(string sourceName,string logName,string productVersion)
		{
            try
            {
                if (!EventLog.SourceExists(sourceName)) EventLog.CreateEventSource(sourceName, logName);
                log = new EventLog(logName);
                log.Source = sourceName;
            }
            catch (Exception)
            {
                StopLog();
            }
		}

		/// <summary>
		/// Set the action to take when an exception is logged into the event
		/// logger. This action will be taken in addition to logging the exception
		/// in the system logs.
		/// </summary>
		/// <param name="showMessageBox">Show a message box with the event</param>
		public static void SetOnExceptionAction(bool showMessageBox)
		{
			g_onExceptionShowMessage = showMessageBox;
		}

		/// <summary>
		/// Stop application wide logging
		/// </summary>
		public static void StopLog()
		{
            try
            {
                Enabled = false;
                if (log != null) log.Close();
                log = null;
            }
            catch (Exception) { }
		}
 
		/// <summary>
		/// Log an information string into the system log.
		/// </summary>
		/// <param name="information">Information string to be logged</param>
		public static void Log(string information) 
		{
			Log(new object(), EventLogEntryType.Information,information);
		}
		public static void Log(object sender, EventLogEntryType LogType, string information) 
		{
            if (sender == null)
            {
                sender = new object();
            }
			if (Enabled)
			{
				if (ShowAll || LogType == EventLogEntryType.Error || LogType == EventLogEntryType.SuccessAudit)
				{
					string origin = sender.GetType().FullName;
					StringBuilder trace = new StringBuilder();

					if (LogType==EventLogEntryType.Error)
					{
						StackTrace t = new StackTrace();
						for(int i=0;i<t.FrameCount;++i)
						{
						    var declaringType = t.GetFrame(i).GetMethod().DeclaringType;
						    if (declaringType != null)
						        trace.Append(declaringType.FullName + "." + t.GetFrame(i).GetMethod().Name + "\r\n");
						}
					}
				    if (log != null) 
				    {
				        try
				        {
				            log.WriteEntry(origin + ": " + information + "\r\n\r\nTRACE:\r\n" + trace,LogType);
				        }
				        catch(Exception)
				        {}
				    }
				    if (OnEvent != null) OnEvent(LogType,sender,trace.ToString(),information);
				}
			}
		}

		public static void Log(Exception exception) 
		{
			Log(exception,"");		
		}

	    /// <summary>
	    /// Log an exception into the system log.
	    /// </summary>
	    /// <param name="exception">Exception to be logged</param>
	    /// <param name="additional"></param>
	    public static void Log(Exception exception, string additional) 
		{
            try
            {
                string name = exception.GetType().FullName;
                string message = exception.Message;
                Exception t = exception;
                int i = 0;
                while (t.InnerException != null)
                {
                    t = t.InnerException;
                    name += " : " + t.GetType().FullName;
                    // message = t.Message;
                    // NKIDD - ADDED
                    message += "\r\n\r\nInnerException #" + i + ":\r\nMessage: " + t.Message + "\r\nSource: " + t.Source + "\r\nStackTrace: " + t.StackTrace;
                    i++;
                }

                name += "\r\n\r\n Additional Info: " + additional + "\r\n" + message;

                if (Enabled)
                {
                    if (log != null)
                    {
                        try
                        {
                            log.WriteEntry(exception.Source + " threw exception: " + exception, EventLogEntryType.Error);
                        }
                        catch (Exception)
                        { }
                    }
                    if (OnEvent != null) OnEvent(EventLogEntryType.Error, exception.Source, exception.StackTrace, name);
                }

                if (g_onExceptionShowMessage)
                {
                    ExceptionForm ef = new ExceptionForm(exception);
                    if (ef.ShowDialog() == DialogResult.OK)
                    {
                        Debugger.Break();
                    }
                    ef.Dispose();
                }
            }
            catch (Exception) { }
		}

	}
}
