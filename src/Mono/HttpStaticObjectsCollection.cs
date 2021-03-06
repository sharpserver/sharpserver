//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections;
using System.Security.Permissions;

namespace HTTP.Net
{
	public sealed class HttpStaticObjectsCollection : ICollection, IEnumerable
	{
		Hashtable _Objects;

		sealed class StaticItem {
			object this_lock = new object();
			
			Type type;
			object instance;
			
			public StaticItem (Type type)
			{
				this.type = type;
			}

			public StaticItem (StaticItem item)
			{
				this.type = item.type;
			}
			
			public object Instance {
				get {
					lock (this_lock) {
						if (instance == null)
							instance = Activator.CreateInstance (type);
					}

					return instance;
				}
			}
		}

		internal HttpStaticObjectsCollection ()
		{
			_Objects = new Hashtable ();
		}

		public object GetObject (string name)
		{
			return this [name];
		}

		public IEnumerator GetEnumerator ()
		{
			return _Objects.GetEnumerator ();
		}

		public void CopyTo (Array array, int index)
		{
			_Objects.CopyTo (array, index);
		}   

		internal IDictionary GetObjects ()
		{
			return _Objects;
		}

		public object this [string name] {
			get {
				StaticItem item = _Objects [name] as StaticItem;
				if (item == null)
					return null;
				
				return item.Instance;
			}
		}

		public int Count {
			get { return _Objects.Count; }
		}

		public bool IsReadOnly {
			get { return true; }
		}

		public bool IsSynchronized {
			get { return false; }
		}

		public bool NeverAccessed {
			get { throw new NotImplementedException (); }
		}

		public object SyncRoot {
			get { return this; }
		}

		internal HttpStaticObjectsCollection Clone ()
		{
			HttpStaticObjectsCollection coll = new HttpStaticObjectsCollection ();
			coll._Objects = new Hashtable ();
			foreach (string key in _Objects.Keys) {
				StaticItem item = new StaticItem ((StaticItem) _Objects [key]);
				coll._Objects [key] = item;
			}
			
			return coll;
		}

        //internal void Add (ObjectTagBuilder tag)
        //{
        //    _Objects.Add (tag.ObjectID, new StaticItem (tag.Type));
        //}
		
		void Set (string name, object obj)
		{
			_Objects [name] = obj;
		}

		public void Serialize (BinaryWriter writer)
		{
			writer.Write (_Objects.Count);
			foreach (string key in _Objects.Keys) {
				writer.Write (key);
				AltSerialization.Serialize (writer, _Objects [key]);
			}
		}

		public static HttpStaticObjectsCollection Deserialize (BinaryReader reader)
		{
			HttpStaticObjectsCollection result = new HttpStaticObjectsCollection ();
			for (int i = reader.ReadInt32 (); i > 0; i--)
				result.Set (reader.ReadString (),
					AltSerialization.Deserialize (reader));

			return result;
		}

		internal byte [] ToByteArray ()
		{
			MemoryStream stream = null;
			try {
				stream = new MemoryStream ();
				Serialize (new BinaryWriter (stream));
				return stream.GetBuffer ();
			} catch {
				throw;
			} finally {
				if (stream != null)
					stream.Close ();
			}
		}

		internal static HttpStaticObjectsCollection FromByteArray (byte [] data)
		{
			HttpStaticObjectsCollection objs = null;
			MemoryStream stream = null;
			try {
				stream = new MemoryStream (data);
				objs = Deserialize (new BinaryReader (stream));
			} catch {
				throw;
			} finally {
				if (stream != null)
					stream.Close ();
			}
			return objs;
		}
	}
}

