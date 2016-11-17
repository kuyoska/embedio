namespace Unosquare.Labs.EmbedIO
{
    using Log;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a Web Server base class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.IDisposable" />
    public abstract class WebServerBase<T> : IDisposable
    {
        /// <summary>
        /// The modules
        /// </summary>
        protected readonly List<IWebModule> _modules = new List<IWebModule>(4);

        /// <summary>
        /// The listener task
        /// </summary>
        protected Task _listenerTask;

        /// <summary>
        /// Gets the underlying HTTP listener.
        /// </summary>
        /// <value>
        /// The listener.
        /// </value>
        public T Listener { get; protected set; }

        /// <summary>
        /// Gets a list of registered modules
        /// </summary>
        /// <value>
        /// The modules.
        /// </value>
        public ReadOnlyCollection<IWebModule> Modules => _modules.AsReadOnly();

        /// <summary>
        /// Gets registered the ISessionModule.
        /// </summary>
        /// <value>
        /// The session module.
        /// </value>
        public ISessionWebModule SessionModule { get; protected set; }

        /// <summary>
        /// Gets the log interface to which this instance will log messages.
        /// </summary>
        /// <value>
        /// The log.
        /// </value>
        public ILog Log { get; protected set; }

        /// <summary>
        /// Runs the asynchronous.
        /// </summary>
        /// <param name="ct">The ct.</param>
        /// <param name="app">The application.</param>
        /// <returns></returns>
        public abstract Task RunAsync(CancellationToken ct = default(CancellationToken), Middleware app = null);

        /// <summary>
        /// Gets the module registered for the given type.
        /// Returns null if no module matches the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Module<T>()
            where T : class, IWebModule
        {
            var module = Modules.FirstOrDefault(m => m.GetType() == typeof(T));
            return module as T;
        }

        /// <summary>
        /// Gets the module registered for the given type.
        /// Returns null if no module matches the given type.
        /// </summary>
        /// <param name="moduleType">Type of the module.</param>
        /// <returns></returns>
        private IWebModule Module(Type moduleType)
        {
            return Modules.FirstOrDefault(m => m.GetType() == moduleType);
        }

        /// <summary>
        /// Registers an instance of a web module. Only 1 instance per type is allowed.
        /// </summary>
        /// <param name="module">The module.</param>
        public void RegisterModule(IWebModule module)
        {
            if (module == null) return;
            var existingModule = Module(module.GetType());
            if (existingModule == null)
            {
                //module.Server = this;
                _modules.Add(module);

                var webModule = module as ISessionWebModule;

                if (webModule != null)
                    SessionModule = webModule;
            }
            else
            {
                Log.WarnFormat("Failed to register module '{0}' because a module with the same type already exists.",
                    module.GetType());
            }
        }

        /// <summary>
        /// Unregisters the module identified by its type.
        /// </summary>
        /// <param name="moduleType">Type of the module.</param>
        public void UnregisterModule(Type moduleType)
        {
            var existingModule = Module(moduleType);
            if (existingModule == null)
            {
                Log.WarnFormat(
                    "Failed to unregister module '{0}' because no module with that type has been previously registered.",
                    moduleType);
            }
            else
            {
                var module = Module(moduleType);
                _modules.Remove(module);
                if (module == SessionModule)
                    SessionModule = null;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            // free managed resources
            if (Listener != null)
            {
                try
                {
                    (Listener as IDisposable)?.Dispose();
                }
                finally
                {
                    Listener = default(T);
                }

                Log.Info("Listener Closed.");
            }
        }
    }
}