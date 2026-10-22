using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Lit les lignes d'un CSV : valeurs entre guillemets (guillemet doublé = guillemet littéral),
    /// retours à la ligne autorisés à l'intérieur des guillemets.
    /// </summary>
    internal sealed class CsvRowReader
    {
        private readonly TextReader _reader;
        private readonly char _delimiter;
        private const int BufferSize = 65536;
        private readonly char[] _buffer = new char[BufferSize];
        private int _length;
        private int _position;

        public CsvRowReader(TextReader reader, char delimiter)
        {
            _reader = reader;
            _delimiter = delimiter;
        }

        /// <summary>Ligne suivante, ou null en fin de fichier. Les lignes vides sont ignorées.</summary>
        public async Task<List<string>?> ReadRowAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = new List<string>();
                var field = new StringBuilder();
                bool inQuotes = false;
                bool wasQuoted = false;
                bool anyChar = false;

                while (true)
                {
                    int c = await ReadCharAsync(cancellationToken);

                    if (c < 0)
                    {
                        if (!anyChar)
                            return null;

                        row.Add(field.ToString());
                        return row;
                    }

                    anyChar = true;

                    if (inQuotes)
                    {
                        if (c == '"')
                        {
                            if (await PeekCharAsync(cancellationToken) == '"')
                            {
                                _position++;
                                field.Append('"');
                            }
                            else
                            {
                                inQuotes = false;
                            }
                        }
                        else
                        {
                            field.Append((char)c);
                        }
                    }
                    else if (c == '"' && field.Length == 0 && !wasQuoted)
                    {
                        inQuotes = true;
                        wasQuoted = true;
                    }
                    else if (c == _delimiter)
                    {
                        row.Add(field.ToString());
                        field.Clear();
                        wasQuoted = false;
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        if (c == '\r' && await PeekCharAsync(cancellationToken) == '\n')
                            _position++;

                        // Ligne vide : on passe à la suivante
                        if (row.Count == 0 && field.Length == 0 && !wasQuoted)
                            break;

                        row.Add(field.ToString());
                        return row;
                    }
                    else
                    {
                        field.Append((char)c);
                    }
                }
            }
        }

        private async ValueTask<int> ReadCharAsync(CancellationToken cancellationToken)
        {
            if (!await FillBufferAsync(cancellationToken))
                return -1;

            return _buffer[_position++];
        }

        private async ValueTask<int> PeekCharAsync(CancellationToken cancellationToken)
        {
            if (!await FillBufferAsync(cancellationToken))
                return -1;

            return _buffer[_position];
        }

        private async ValueTask<bool> FillBufferAsync(CancellationToken cancellationToken)
        {
            if (_position < _length)
                return true;

            _length = await _reader.ReadAsync(_buffer.AsMemory(), cancellationToken);
            _position = 0;
            return _length > 0;
        }
    }
}
