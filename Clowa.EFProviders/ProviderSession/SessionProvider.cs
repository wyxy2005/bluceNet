using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.SessionState;
using System.Web;
using System.IO;
using Clowa.EFProviders.Resources;
using System.Collections.Specialized;

namespace Clowa.EFProviders.ProviderSession
{
    public class SessionProvider: SessionStateStoreProviderBase
    {
        private long _lastSessionPurgeTicks;
        private const int ITEM_SHORT_LENGTH = 0x1b58;//7000
        private const double SessionExpiresFrequencyCheckInSeconds = 30.0;
        private static string AppendAppIdHash(string id)
        {
            string str2 = GetAppDomainAppId().GetHashCode().ToString("X8", CultureInfo.InvariantCulture);
            if (id.EndsWith(str2, StringComparison.OrdinalIgnoreCase))
            {
                return id;
            }
            return (id + str2);
        }

        internal bool CanPurge()
        {
            return (TimeSpan.FromTicks(DateTime.UtcNow.Ticks - this.LastSessionPurgeTicks).TotalSeconds > 30.0);
        }
        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            HttpStaticObjectsCollection staticObjects = null;
            if (context != null)
            {
                staticObjects = SessionStateUtility.GetSessionStaticObjects(context);
            }
            return new SessionStateStoreData(new SessionStateItemCollection(), staticObjects, timeout);
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            if (id.Length > SessionIDManager.SessionIDMaxLength)
            {
                throw new ArgumentException(ProviderResources.Session_id_too_long);
            }
            id = AppendAppIdHash(id);
            using (SessionContext context2 = ModelHelper.CreateSessionContext(this.ConnectionString))
            {
                if (context2.Sessions.Find(new object[] { id }) == null)
                {
                    Session session = NewSession(id, timeout);
                    SessionStateStoreData item = new SessionStateStoreData(new SessionStateItemCollection(), SessionStateUtility.GetSessionStaticObjects(context), session.Timeout);
                    SaveItemToSession(session, item, this.CompressionEnabled);
                    context2.Sessions.Add(session);
                    context2.SaveChanges();
                }
            }
        }

        private static SessionStateStoreData Deserialize(HttpContext context, Stream stream)
        {
            int num;
            SessionStateItemCollection items;
            HttpStaticObjectsCollection sessionStaticObjects;
            try
            {
                BinaryReader reader = new BinaryReader(stream);
                num = reader.ReadInt32();
                bool flag = reader.ReadBoolean();
                bool flag2 = reader.ReadBoolean();
                if (flag)
                {
                    items = SessionStateItemCollection.Deserialize(reader);
                }
                else
                {
                    items = new SessionStateItemCollection();
                }
                if (flag2)
                {
                    sessionStaticObjects = HttpStaticObjectsCollection.Deserialize(reader);
                }
                else
                {
                    sessionStaticObjects = SessionStateUtility.GetSessionStaticObjects(context);
                }
                if (reader.ReadByte() != 0xff)
                {
                    throw new HttpException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Invalid_session_state, new object[0]));
                }
            }
            catch (EndOfStreamException)
            {
                throw new HttpException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Invalid_session_state, new object[0]));
            }
            return new SessionStateStoreData(items, sessionStaticObjects, num);
        }

        private static SessionStateStoreData DeserializeStoreData(HttpContext context, Stream stream, bool compressionEnabled)
        {
            if (compressionEnabled)
            {
                using (DeflateStream stream2 = new DeflateStream(stream, CompressionMode.Decompress, true))
                {
                    return Deserialize(context, stream2);
                }
            }
            return Deserialize(context, stream);
        }

        public override void Dispose()
        {
        }

        private SessionStateStoreData DoGet(HttpContext context, string id, bool exclusive, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            if (id.Length > SessionIDManager.SessionIDMaxLength)
            {
                throw new ArgumentException(ProviderResources.Session_id_too_long);
            }
            id = AppendAppIdHash(id);
            locked = false;
            lockAge = TimeSpan.Zero;
            lockId = null;
            actions = SessionStateActions.None;
            using (SessionContext context2 = ModelHelper.CreateSessionContext(this.ConnectionString))
            {
                Session session = context2.Sessions.Find(new object[] { id });
                if ((session != null) && (session.Expires > DateTime.UtcNow))
                {
                    session.Expires = DateTime.UtcNow.AddMinutes((double)session.Timeout);
                    locked = session.Locked;
                    lockId = session.LockCookie;
                    SessionStateStoreData data = null;
                    if (locked)
                    {
                        TimeSpan span = (TimeSpan)(DateTime.UtcNow - session.LockDate);
                        lockAge = TimeSpan.FromSeconds((double)span.Seconds);
                    }
                    else
                    {
                        if (exclusive)
                        {
                            session.Locked = true;
                            session.LockDate = DateTime.UtcNow;
                        }
                        byte[] sessionItem = session.SessionItem;
                        if (session.Flags == 1)
                        {
                            data = InitializeSessionItem(context, session, this.CompressionEnabled);
                        }
                        else
                        {
                            using (MemoryStream stream = new MemoryStream(sessionItem))
                            {
                                data = DeserializeStoreData(context, stream, this.CompressionEnabled);
                            }
                        }
                    }
                    context2.SaveChanges();
                    return data;
                }
            }
            return null;
        }

        public override void EndRequest(HttpContext context)
        {
            this.PurgeIfNeeded();
        }

        private static string GetAppDomainAppId()
        {
            string data = AppDomain.CurrentDomain.GetData(".appId") as string;
            if (data != null)
            {
                return data;
            }
            return HttpRuntime.AppDomainAppId;
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            return this.DoGet(context, id, false, out locked, out lockAge, out lockId, out actions);
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            return this.DoGet(context, id, true, out locked, out lockAge, out lockId, out actions);
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (string.IsNullOrEmpty(name))
            {
                name = "DefaultSessionStateProvider";
            }
            base.Initialize(name, config);
            this.ConnectionString = ModelHelper.GetConnectionString(config["connectionStringName"]);
            config.Remove("connectionStringName");
            try
            {
                SessionStateSection section = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");
                this.CompressionEnabled = section.CompressionEnabled;
            }
            catch (SecurityException)
            {
            }
            if (config.Count > 0)
            {
                string key = config.GetKey(0);
                if (!string.IsNullOrEmpty(key))
                {
                    throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_unrecognized_attribute, new object[] { key }));
                }
            }
        }

        public override void InitializeRequest(HttpContext context)
        {
        }

        private static SessionStateStoreData InitializeSessionItem(HttpContext context, Session session, bool compression)
        {
            SessionStateStoreData item = new SessionStateStoreData(new SessionStateItemCollection(), SessionStateUtility.GetSessionStaticObjects(context), session.Timeout);
            SaveItemToSession(session, item, compression);
            session.Flags = 0;
            return item;
        }

        private static Session NewSession(string id, int timeout)
        {
            Session session = new Session();
            DateTime utcNow = DateTime.UtcNow;
            session.Created = utcNow;
            session.SessionId = id;
            session.Timeout = timeout;
            session.Expires = utcNow.AddMinutes((double)timeout);
            session.Locked = false;
            session.LockDate = utcNow;
            session.LockCookie = 0;
            session.Flags = 0;
            return session;
        }

        private void PurgeExpiredSessions()
        {
            try
            {
                using (SessionContext context = ModelHelper.CreateSessionContext(this.ConnectionString))
                {
                    foreach (Session session in from s in context.Sessions
                                                where s.Expires < DateTime.UtcNow
                                                select s)
                    {
                        context.Sessions.Remove(session);
                    }
                    context.SaveChanges();
                    this.LastSessionPurgeTicks = DateTime.UtcNow.Ticks;
                }
            }
            catch
            {
            }
        }

        private void PurgeIfNeeded()
        {
            Action action = null;
            if (this.CanPurge())
            {
                if (action == null)
                {
                    action = delegate
                    {
                        this.PurgeExpiredSessions();
                    };
                }
                new Task(action).Start();
            }
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            if (id.Length > SessionIDManager.SessionIDMaxLength)
            {
                throw new ArgumentException(ProviderResources.Session_id_too_long);
            }
            using (SessionContext context2 = ModelHelper.CreateSessionContext(this.ConnectionString))
            {
                ReleaseItemNoSave(context2, id, lockId);
                context2.SaveChanges();
            }
        }

        private static void ReleaseItemNoSave(SessionContext db, string id, object lockId)
        {
            id = AppendAppIdHash(id);
            Session session = db.Sessions.Find(new object[] { id });
            if (((session != null) && session.Locked) && (session.LockCookie == ((int)lockId)))
            {
                session.Locked = false;
            }
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            if (id.Length > SessionIDManager.SessionIDMaxLength)
            {
                throw new ArgumentException(ProviderResources.Session_id_too_long);
            }
            id = AppendAppIdHash(id);
            using (SessionContext context2 = ModelHelper.CreateSessionContext(this.ConnectionString))
            {
                Session entity = context2.Sessions.Find(new object[] { id });
                if ((entity != null) && (entity.LockCookie == ((int)lockId)))
                {
                    context2.Sessions.Remove(entity);
                    context2.SaveChanges();
                }
            }
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            if (id.Length > SessionIDManager.SessionIDMaxLength)
            {
                throw new ArgumentException(ProviderResources.Session_id_too_long);
            }
            id = AppendAppIdHash(id);
            using (SessionContext context2 = ModelHelper.CreateSessionContext(this.ConnectionString))
            {
                Session session = context2.Sessions.Find(new object[] { id });
                if (session != null)
                {
                    session.Expires = DateTime.UtcNow.AddMinutes((double)session.Timeout);
                    context2.SaveChanges();
                }
            }
        }

        private static void SaveItemToSession(Session session, SessionStateStoreData item, bool compression)
        {
            byte[] buf = null;
            int length = 0;
            SerializeStoreData(item, 0x1b58, out buf, out length, compression);
            session.SessionItem = buf;
        }

        private static void Serialize(SessionStateStoreData item, Stream stream)
        {
            bool flag = true;
            bool flag2 = true;
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(item.Timeout);
            if ((item.Items == null) || (item.Items.Count == 0))
            {
                flag = false;
            }
            writer.Write(flag);
            if ((item.StaticObjects == null) || item.StaticObjects.NeverAccessed)
            {
                flag2 = false;
            }
            writer.Write(flag2);
            if (flag)
            {
                ((SessionStateItemCollection)item.Items).Serialize(writer);
            }
            if (flag2)
            {
                item.StaticObjects.Serialize(writer);
            }
            writer.Write((byte)0xff);
        }

        private static void SerializeStoreData(SessionStateStoreData item, int initialStreamSize, out byte[] buf, out int length, bool compressionEnabled)
        {
            using (MemoryStream stream = new MemoryStream(initialStreamSize))
            {
                Serialize(item, stream);
                if (compressionEnabled)
                {
                    byte[] buffer = stream.GetBuffer();
                    int count = (int)stream.Length;
                    stream.SetLength(0);
                    using (DeflateStream stream2 = new DeflateStream(stream, CompressionMode.Compress, true))
                    {
                        stream2.Write(buffer, 0, count);
                    }
                    stream.WriteByte(0xff);
                }
                buf = stream.GetBuffer();
                length = (int)stream.Length;
            }
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            if (id.Length > SessionIDManager.SessionIDMaxLength)
            {
                throw new ArgumentException(ProviderResources.Session_id_too_long);
            }
            id = AppendAppIdHash(id);
            using (SessionContext context2 = ModelHelper.CreateSessionContext(this.ConnectionString))
            {
                Session entity = context2.Sessions.Find(new object[] { id });
                if (entity == null)
                {
                    if (!newItem)
                    {
                        if (entity == null)
                        {
                            throw new InvalidOperationException(ProviderResources.Session_not_found);
                        }
                    }
                    else
                    {
                        entity = NewSession(id, item.Timeout);
                        context2.Sessions.Add(entity);
                    }
                }
                else
                {
                    if (lockId == null)
                    {
                        entity.LockCookie = 0;
                    }
                    else
                    {
                        entity.LockCookie = (int)lockId;
                    }
                    entity.Locked = false;
                    entity.Timeout = item.Timeout;
                }
                SaveItemToSession(entity, item, this.CompressionEnabled);
                context2.SaveChanges();
            }
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }

        private bool CompressionEnabled { get; set; }

        private ConnectionStringSettings ConnectionString { get; set; }

        internal long LastSessionPurgeTicks
        {
            get
            {
                return Interlocked.Read(ref this._lastSessionPurgeTicks);
            }
            set
            {
                Interlocked.Exchange(ref this._lastSessionPurgeTicks, value);
            }
        }
    }
}
