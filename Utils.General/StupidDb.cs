using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NLog;

namespace Utils.General
{
    /// <summary>
    /// Stupid implementation of a single-file, document-based database.
    /// File can contain multiple instances of a single "document" type.
    /// "Document" type can contain arbitrary data that can be parsed as JSON.
    /// </summary>
    /// <remarks>
    /// Database file is human-readable JSON text, but shouldn't be manually edited when the program is running.
    /// </remarks>
    /// <remarks>
    /// "Document" type must contain one ID field (or property) that uniquely identifies each document.
    /// ID must be `string` and must be specified by `[StupidDbId]` attribute.
    /// </remarks>
    /// <remarks>
    /// Shouldn't be used to process big data.
    /// </remarks>
    public sealed class StupidDb<T>
    {
        readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly string _filePath;
        readonly PropertyInfo _idProperty;
        readonly Dictionary<string, T> _ramCopy;

        /// <summary>
        /// Instantiate with a path to the database file.
        /// </summary>
        /// <param name="filePath">Path to the database file.</param>
        public StupidDb(string filePath)
        {
            filePath.ThrowIfNullOrEmpty(nameof(filePath));

            _filePath = filePath;
            _idProperty = StupidDbIdAttribute.FindIdProperty<T>();
            _ramCopy = new Dictionary<string, T>();
        }

        /// <summary>
        /// Reset both RAM copy and database file to an empty state.
        /// </summary>
        public void Reset()
        {
            _ramCopy.Clear();
            Write();
        }

        /// <summary>
        /// Reset RAM copy.
        /// </summary>
        public void Clear()
        {
            _ramCopy.Clear();
        }

        /// <summary>
        /// Read the database file and cache it in the RAM.
        /// If the file is not found, create an empty JSON file.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Read()
        {
            _ramCopy.Clear();

            if (!File.Exists(_filePath))
            {
                var emptyText = JsonConvert.SerializeObject(_ramCopy);
                File.WriteAllText(_filePath, emptyText);
                return;
            }

            try
            {
                var fileText = File.ReadAllText(_filePath);
                var copy = JsonConvert.DeserializeObject<Dictionary<string, T>>(fileText);
                _ramCopy.AddRange(copy);
            }
            catch (Exception e)
            {
                Log.Warn(e);
                var emptyText = JsonConvert.SerializeObject(_ramCopy);
                File.WriteAllText(_filePath, emptyText);
            }
        }

        /// <summary>
        /// Get a document with the ID.
        /// </summary>
        /// <param name="id">ID of document.</param>
        /// <param name="document">Document object if found otherwise null.</param>
        /// <returns>Document found in the database specified by the ID.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryQuery(string id, out T document)
        {
            return _ramCopy.TryGetValue(id, out document);
        }

        public T QueryOrDefault(string id, T defaultValue = default)
        {
            return TryQuery(id, out var d) ? d : defaultValue;
        }

        /// <summary>
        /// Get all documents in this database.
        /// </summary>
        /// <returns>All documents found in the database.</returns>
        public IEnumerable<T> QueryAll()
        {
            return _ramCopy.Values;
        }

        /// <summary>
        /// Insert (or update) a document.
        /// </summary>
        /// <remarks>
        /// An existing document will be overwritten by the new document with the ID if any.
        /// </remarks>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Insert(T document)
        {
            var id = GetId(document);
            _ramCopy[id] = document;
        }

        /// <summary>
        /// Insert (or update) documents.
        /// </summary>
        /// <remarks>
        /// Existing documents will be overwritten by new documents with the ID if any.
        /// </remarks>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void InsertAll(IEnumerable<T> documents)
        {
            foreach (var document in documents)
            {
                Insert(document);
            }
        }

        /// <summary>
        /// Write the RAM copy to the database file.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Write()
        {
            var text = JsonConvert.SerializeObject(_ramCopy, Formatting.Indented);
            File.WriteAllText(_filePath, text);
        }

        string GetId(T document)
        {
            return (string) _idProperty.GetValue(document);
        }

        public bool Contains(string id)
        {
            return _ramCopy.ContainsKey(id);
        }
    }
}