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
using System.Reflection;
using System.Collections;

namespace OpenSource.Utilities
{
	/// <summary>
	/// Summary description for WeakEvent.
	/// </summary>
	public sealed class WeakEvent
	{
		private ArrayList EventList = new ArrayList();
		private object EventLock = new object();

		public WeakEvent()
		{	
			InstanceTracker.Add(this);
		}

		public void Fire()
		{
			Fire(new object[0]);
		}
		public void Fire(object sender)
		{
			Fire(new[]{sender});
		}
		public void Fire(object sender, object arg1)
		{
			Fire(new[]{sender,arg1});
		}
		public void Fire(object sender, object arg1, object arg2)
		{
			Fire(new[]{sender,arg1,arg2});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3)
		{
			Fire(new[]{sender,arg1,arg2,arg3});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11,arg12});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11,arg12,arg13});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11,arg12,arg13,arg14});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11,arg12,arg13,arg14,arg15});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11,arg12,arg13,arg14,arg15,arg16});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11,arg12,arg13,arg14,arg15,arg16,arg17});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11,arg12,arg13,arg14,arg15,arg16,arg17,arg18});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18, object arg19)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11,arg12,arg13,arg14,arg15,arg16,arg17,arg18,arg19});
		}
		public void Fire(object sender, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18, object arg19, object arg20)
		{
			Fire(new[]{sender,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10,arg11,arg12,arg13,arg14,arg15,arg16,arg17,arg18,arg19,arg20});
		}


		public void Fire(object[] args)
		{
			object[] ObjectRefs = (object[])EventList.ToArray(typeof(object));

			foreach(object[] ObjectRef in ObjectRefs)
			{
				WeakReference W = (WeakReference)ObjectRef[1];
				object o = W.Target;
				bool IsStatic = (bool)ObjectRef[2];
				MethodInfo mi = (MethodInfo)ObjectRef[0];
				if (W.IsAlive || IsStatic)
				{
					try
					{
						mi.Invoke(o, args);
					}
					catch(Exception e)
					{
						EventLogger.Log(e);
					}
				}
				else
				{
					lock(EventLock)
					{
						EventList.Remove(ObjectRef);
					}
				}
			}
			
		}

		public void Register(object applicant, string methodName)
		{
			lock(EventLock)
			{
				EventList.Add(new object[]{applicant.GetType().GetMethod(methodName,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance),new WeakReference(applicant),false});
			}
		}
		public void Register(Delegate handler)
		{
			lock(EventLock)
			{
				EventList.Add(new object[]{handler.Method,new WeakReference(handler.Target),handler.Target==null});
			}
		}
		public void UnRegisterAll()
		{
			lock(EventLock)
			{
				EventList.Clear();
			}
		}
		public void UnRegister(Delegate handler)
		{
			UnRegister(handler.Target,handler.Method.Name);
		}
		public void UnRegister(object applicant, string methodName)
		{
			lock(EventLock)
			{
				object[] ObjectRefs = (object[])EventList.ToArray(typeof(object));
				foreach(object[] ObjectRef in ObjectRefs)
				{
					WeakReference W = (WeakReference)ObjectRef[1];
					object o;
					try
					{
						o = W.Target;
					}
					catch
					{
						o = null;
					}

					MethodInfo mi = (MethodInfo)ObjectRef[0];
					if (o!=null && W.IsAlive)
					{
						if (o==applicant && mi.Name==methodName)
						{
							EventList.Remove(ObjectRef);
							break;			
						}
					}
					else
					{
						EventList.Remove(ObjectRef);				
					}
					
				}
			}
		}
	}
}
