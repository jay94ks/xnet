using System.Buffers;
using XnetStreams.Internals.Packets;

namespace XnetStreams.Internals
{
    /// <summary>
    /// Stream Extender.
    /// </summary>
    internal class StreamExtender : Xnet.BasicPacketProvider<StreamExtender>, Xnet.Extender
    {
        private readonly PacketDispatcher m_Dispatcher = new();
        private readonly StreamRegistry m_Registry = new();
        private StreamDelegate m_Delegate;

        /// <summary>
        /// Concat the delegate to end of previous delegate.
        /// </summary>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        public StreamExtender Use(StreamDelegate Delegate)
        {
            m_Delegate = m_Delegate.Concat(Delegate);
            return this;
        }

        /// <summary>
        /// Test whether the disk is full or not.
        /// </summary>
        /// <returns></returns>
        private static bool IsDiskFull()
        {
            var AppRoot = Path.GetDirectoryName(
                typeof(StreamExtender).Assembly.Location);

            try
            {
                var Drive = new DriveInfo(AppRoot);
                return Drive.AvailableFreeSpace <= 0;
            }
            catch
            {
                try
                {
                    var Dir = new DirectoryInfo(AppRoot);
                    var Drive = new DriveInfo(Dir.Root.FullName);
                    return Drive.AvailableFreeSpace <= 0;
                }
                catch { }
            }

            return false;
        }

        /// <inheritdoc/>
        protected override void MapTypes()
        {
            Map<PKT_OPEN>("stream.open");
            Map<PKT_FLUSH>("stream.flush");
            Map<PKT_CLOSE>("stream.close");
            Map<PKT_READ>("stream.read");
            Map<PKT_WRITE>("stream.write");
            Map<PKT_SEEK>("stream.seek");
            Map<PKT_TELL>("stream.tell");
            Map<PKT_SETLENGTH>("stream.setlen");
            Map<PKT_READTIMEOUT>("stream.readtimeout");
            Map<PKT_WRITETIMEOUT>("stream.writetimeout");

            Map<PKT_OPEN_RESULT>("stream.open.res");
            Map<PKT_FLUSH_RESULT>("stream.flush.res");
            Map<PKT_CLOSE_RESULT>("stream.close.res");
            Map<PKT_READ_RESULT>("stream.read.res");
            Map<PKT_WRITE_RESULT>("stream.write.res");
            Map<PKT_SEEK_RESULT>("stream.seek.res");
            Map<PKT_TELL_RESULT>("stream.tell.res");
            Map<PKT_SETLENGTH_RESULT>("stream.setlen.res");
            Map<PKT_READTIMEOUT_RESULT>("stream.readtimeout.res");
            Map<PKT_WRITETIMEOUT_RESULT>("stream.writetimeout.res");
        }

        /// <summary>
        /// Get the stream extender.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <returns></returns>
        public static StreamExtender Get(Xnet Xnet)
        {
            return Xnet.GetExtender<StreamExtender>();
        }

        /// <summary>
        /// Send a request packet and wait its response asynchronously.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="Xnet"></param>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        public async Task<TResult> RequestAsync<TResult>(Xnet Xnet, PKT_BASE Request, CancellationToken Token = default) where TResult : PKT_BASE_RESULT
        {
            using var Reservation = m_Dispatcher.Reserve();
            Request.TraceId = Reservation.TraceId;

            if (await Xnet.EmitAsync(Request, Token) == false)
            {
                Token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("the remote connection is not alive.");
            }

            using (Xnet.Closing.Register(Reservation.Dispose, false))
            {
                var Packet = await Reservation.WaitAsync(Token);
                if (Packet is null)
                    throw new InvalidOperationException("the remote connection is closed before completion.");

                if (Packet is not TResult Result)
                    throw new InvalidDataException("the dispatched result is not correct.");

                return Result;
            }
        }

        /// <summary>
        /// Open the stream with options asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Options"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        public async Task<RemoteStream> OpenAsync(Xnet Xnet, StreamOptions Options, CancellationToken Token = default)
        {
            using var Reservation = m_Dispatcher.Reserve();
            var Open = new PKT_OPEN
            {
                TraceId = Reservation.TraceId,
                Timeout = Options.Timeout,
                ReadTimeout = Options.ReadTimeout,
                WriteTimeout = Options.WriteTimeout,
                Path = Options.Path ?? string.Empty,
                Mode = Options.Mode,
                Access = Options.Access,
                Share = Options.Share,
                Extras = Options.Extras,
            };

            if (await Xnet.EmitAsync(Open, Token) == false)
            {
                Token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("the remote connection is not alive.");
            }

            using (Xnet.Closing.Register(Reservation.Dispose, false))
            {
                var Handled = false;
                try
                {
                    var Packet = await Reservation.WaitAsync(Token);
                    if (Packet is null)
                        throw new InvalidOperationException("the remote connection is closed before completion.");

                    if (Packet is not PKT_OPEN_RESULT Result)
                        throw new InvalidDataException("the dispatched result is not correct.");

                    Handled = true;
                    if (Result.Status == StreamStatus.Ok)
                    {
                        return new RemoteStream(Xnet, Result);
                    }

                    throw new StreamException(Result.Status);
                }

                finally
                {
                    if (Handled == false)
                    {
                        var Packet = await Reservation.TryPeekAsync();
                        if (Packet != null)
                        {
                            await CloseUnhandledStreamAsync(Xnet, Packet.Id);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Close the unhandled stream asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Reservation"></param>
        /// <returns></returns>
        private static async ValueTask CloseUnhandledStreamAsync(Xnet Xnet, Guid Id)
        {
            var Close = new PKT_CLOSE
            {
                Id = Id,
                TraceId = Guid.Empty,
                FlushRequired = false,
            };

            await Xnet.EmitAsync(Close);
        }

        /// <summary>
        /// Handle the packet asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Packet"></param>
        /// <returns></returns>
        public async Task HandleResultAsync(Xnet Xnet, PKT_BASE_RESULT Packet)
        {
            if (Packet.TraceId == Guid.Empty)
                return;

            var Success = m_Dispatcher.Dispatch(Packet);
            if (Success == false)
            {
                switch (Packet)
                {
                    case PKT_OPEN_RESULT OpenResult:
                        // --> failed to dispatch open result.
                        if (OpenResult.Status == StreamStatus.Ok)
                            await CloseUnhandledStreamAsync(Xnet, OpenResult.Id);

                        break;

                    default:
                        break;

                    //case PKT_CLOSE_RESULT:
                    //    break;

                    //case PKT_FLUSH_RESULT:
                    //case PKT_READ_RESULT:
                    //case PKT_WRITE_RESULT:
                    //case PKT_SEEK_RESULT:
                    //case PKT_TELL_RESULT:
                    //case PKT_SETLENGTH_RESULT:
                    //    break;

                }

                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Open the stream from the packet asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Packet"></param>
        /// <returns></returns>
        public async Task OpenAsync(Xnet Xnet, PKT_OPEN Packet)
        {
            var Context = new StreamContext
            {
                Connection = Xnet,
                Stream = null,
                Status = StreamStatus.PathNotFound,
                Timeout = Packet.Timeout,
                Path = Packet.Path,
                Mode = Packet.Mode,
                Access = Packet.Access,
                Share = Packet.Share,
                Extras = Packet.Extras,
            };

            var Result = new PKT_OPEN_RESULT
            {
                Id = Guid.Empty,
                TraceId = Packet.TraceId,
                Status = StreamStatus.PathNotFound,
                CanRead = false,
                CanWrite = false,
                CanSeek = false,
                Cursor = -1,
                Length = -1,
                ReadTimeout = -1,
                WriteTimeout = -1,
            };

            // --> final request handler.
            Task FinalRequestHandler()
            {
                return Task.CompletedTask;
            }

            try
            {
                if (m_Delegate != null)
                    await m_Delegate.Invoke(Context, FinalRequestHandler);

                else
                    await FinalRequestHandler();
            }
            catch
            {
                if (Context.Stream != null)
                {
                    try { Context.Stream.Close(); } catch { }
                    try { await Context.Stream.DisposeAsync(); } catch { }

                    Context.Stream = null;
                }

                Context.Status = StreamStatus.UnhandledException;
                throw;
            }

            finally
            {

                var Stream = Context.Stream;
                if (Stream is null)
                {
                    // --> status is OK: implemented in bad-way.
                    if (Context.Status != StreamStatus.Ok)
                        Result.Status = Context.Status;

                    else
                        Result.Status = StreamStatus.PathNotFound;
                }

                else
                {
                    var Reg = m_Registry.Register(Xnet, Stream);

                    // --> registration id.
                    Result.Id = Reg.Id;
                    Result.Status = StreamStatus.Ok;

                    // --> set the result properties safely.
                    SetTimeoutToStream(Packet, Stream);
                    SetCapabilitiesFromStream(Result, Stream);
                }

                await Xnet.EmitAsync(Result);
            }
        }

        /// <summary>
        /// Set the timeout values to the stream.
        /// </summary>
        /// <param name="Packet"></param>
        /// <param name="Stream"></param>
        private static void SetTimeoutToStream(PKT_OPEN Packet, Stream Stream)
        {
            try
            {
                if (Stream.CanTimeout)
                {
                    try
                    {
                        if (Stream.CanRead && Packet.ReadTimeout > 0)
                            Stream.ReadTimeout = Packet.ReadTimeout;
                    }
                    catch { }

                    try
                    {
                        if (Stream.CanWrite && Packet.WriteTimeout > 0)
                            Stream.WriteTimeout = Packet.WriteTimeout;
                    }
                    catch { }
                }
            }

            catch { }
        }

        /// <summary>
        /// Set capabilities from the stream.
        /// </summary>
        /// <param name="Result"></param>
        /// <param name="Stream"></param>
        private static void SetCapabilitiesFromStream(PKT_OPEN_RESULT Result, Stream Stream)
        {
            try { Result.CanRead = Stream.CanRead; }
            catch { Result.CanRead = false; }

            try { Result.CanWrite = Stream.CanWrite; }
            catch { Result.CanWrite = false; }

            try { Result.CanSeek = Stream.CanSeek; }
            catch { Result.CanSeek = false; }

            try
            {
                if (Result.CanSeek)
                {
                    Result.Cursor = Stream.Position;
                    Result.Length = Stream.Length;
                }
            }
            catch { }
            try
            {
                if (Stream.CanTimeout)
                {
                    Result.ReadTimeout = -1;
                    Result.WriteTimeout = -1;

                    try
                    {
                        if (Stream.CanRead)
                            Result.ReadTimeout = Stream.ReadTimeout;
                    }
                    catch { Result.ReadTimeout = -1; }

                    try
                    {
                        if (Stream.CanWrite)
                            Result.WriteTimeout = Stream.WriteTimeout;
                    }
                    catch { Result.WriteTimeout = -1; }
                }
            }
            catch { }

        }

        /// <summary>
        /// Close the stream asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        internal async Task CloseAsync(Xnet Xnet, PKT_CLOSE Close)
        {
            var Result = new PKT_CLOSE_RESULT
            {
                Id = Close.Id,
                TraceId = Close.TraceId,
                Status = StreamStatus.Ok
            };

            try
            {
                var Reg = m_Registry.Get(Close.Id);
                if (Reg is null || Reg.Xnet != Xnet)
                {
                    Result.Status = StreamStatus.NotAStream;
                    return;
                }

                Result.Status = StreamStatus.UnhandledException;
                if (Close.FlushRequired)
                {
                    try { await Reg.Stream.FlushAsync(); }
                    catch
                    {
                    }
                }

                await Reg.DisposeAsync();
                Result.Status = StreamStatus.Ok;
            }

            finally
            {
                if (Result.TraceId != Guid.Empty)
                    await Xnet.EmitAsync(Result);
            }
        }

        /// <summary>
        /// Read the stream asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Read"></param>
        /// <returns></returns>
        internal async Task ReadAsync(Xnet Xnet, PKT_READ Read)
        {
            var Result = new PKT_READ_RESULT
            {
                Id = Read.Id,
                TraceId = Read.TraceId,
                Status = StreamStatus.NotImplemented,
                Data = Array.Empty<byte>()
            };

            try
            {
                var Reg = m_Registry.Get(Read.Id);
                if (Reg is null || Reg.Xnet != Xnet)
                {
                    Result.Status = StreamStatus.NotAStream;
                    return;
                }

                if (Read.Size <= 0)
                {
                    Result.Status = StreamStatus.Ok;
                    return;
                }

                if (Read.Size >= ushort.MaxValue)
                    Read.Size = ushort.MaxValue / 2;

                if (Reg.Stream.CanRead == false)
                {
                    Result.Status = StreamStatus.NotSupported;
                    return;
                }


                var Retval = new byte[Read.Size];
                var Buffer = ArrayPool<byte>.Shared.Rent(8192);
                var Offset = 0;

                try
                {
                    while (Offset < Retval.Length)
                    {
                        var Segment = new ArraySegment<byte>(Buffer, 0,
                            Math.Min(Buffer.Length, Retval.Length - Offset));

                        try
                        {
                            var Length = await Reg.Stream.ReadAsync(Segment, Xnet.Closing);
                            if (Length <= 0)
                                break;

                            Segment.Slice(0, Length).CopyTo(Retval, Offset);
                            Offset += Length;
                        }

                        catch(Exception Exception)
                        {
                            if (Exception is OperationCanceledException Oce &&
                                Oce.CancellationToken == Xnet.Closing)
                            {
                                Result = null;
                                return;
                            }

                            if (Exception is NotImplementedException)
                            {
                                Result.Status = StreamStatus.NotImplemented;
                                return;
                            }

                            if (Exception is NotSupportedException)
                            {
                                Result.Status = StreamStatus.NotSupported;
                                return;
                            }

                            if (Exception is InvalidOperationException)
                            {
                                Result.Status = StreamStatus.InvalidOperation;
                                return;
                            }

                            if (Exception is TimeoutException)
                            {
                                Result.Status = StreamStatus.Timeout;
                                return;
                            }

                            if (Offset > 0)
                                break;

                            if (Exception is EndOfStreamException)
                            {
                                Result.Status = StreamStatus.EndOfStream;
                                return;
                            }

                            Result.Status = StreamStatus.Broken;
                            return;
                        }
                    }

                    if (Offset <= 0)
                    {
                        Result.Status = StreamStatus.Ok;
                        Result.Data = Array.Empty<byte>();
                        return;
                    }

                    Array.Resize(ref Retval, Offset);
                    Result.Status = StreamStatus.Ok;
                    Result.Data = Retval;
                }

                finally
                {
                    ArrayPool<byte>.Shared.Return(Buffer);
                }
            }

            finally
            {
                if (Result != null && Result.TraceId != Guid.Empty)
                    await Xnet.EmitAsync(Result);
            }
        }

        /// <summary>
        /// Seek the stream asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Seek"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal async Task SeekAsync(Xnet Xnet, PKT_SEEK Seek)
        {
            var Result = new PKT_SEEK_RESULT
            {
                Id = Seek.Id,
                TraceId = Seek.TraceId,
                Status = StreamStatus.NotImplemented
            };

            try
            {
                var Reg = m_Registry.Get(Seek.Id);
                if (Reg is null || Reg.Xnet != Xnet)
                {
                    Result.Status = StreamStatus.NotAStream;
                    return;
                }

                if (Reg.Stream.CanSeek == false)
                {
                    Result.Status = StreamStatus.NotSupported;
                    return;
                }

                try
                {
                    Reg.Stream.Seek(Seek.Cursor, Seek.Origin);
                    Result.Status = StreamStatus.Ok;
                    Result.Cursor = Reg.Stream.Position;
                }

                catch (Exception Exception)
                {
                    if (Exception is NotImplementedException)
                        Result.Status = StreamStatus.NotImplemented;

                    else if (Exception is NotSupportedException)
                        Result.Status = StreamStatus.NotSupported;

                    else if (Exception is InvalidOperationException)
                        Result.Status = StreamStatus.InvalidOperation;

                    else if (Exception is TimeoutException)
                        Result.Status = StreamStatus.Timeout;

                    else
                        Result.Status = StreamStatus.Broken;
                }
            }

            finally
            {
                if (Result.TraceId != Guid.Empty)
                    await Xnet.EmitAsync(Result);
            }
        }

        /// <summary>
        /// Set the stream's length asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="SetLength"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal async Task SetLengthAsync(Xnet Xnet, PKT_SETLENGTH SetLength)
        {
            var Result = new PKT_SETLENGTH_RESULT
            {
                Id = SetLength.Id,
                TraceId = SetLength.TraceId,
                Status = StreamStatus.NotImplemented
            };

            try
            {
                var Reg = m_Registry.Get(SetLength.Id);
                if (Reg is null || Reg.Xnet != Xnet)
                {
                    Result.Status = StreamStatus.NotAStream;
                    return;
                }

                if (Reg.Stream.CanSeek == false)
                {
                    Result.Status = StreamStatus.NotSupported;
                    return;
                }

                try
                {
                    Reg.Stream.SetLength(SetLength.Length);
                    Result.Status = StreamStatus.Ok;
                    Result.Length = Reg.Stream.Length;
                }

                catch (Exception Exception)
                {
                    if (Exception is NotImplementedException)
                        Result.Status = StreamStatus.NotImplemented;

                    else if (Exception is NotSupportedException)
                        Result.Status = StreamStatus.NotSupported;

                    else if (Exception is InvalidOperationException)
                        Result.Status = StreamStatus.InvalidOperation;

                    else if (Exception is TimeoutException)
                        Result.Status = StreamStatus.Timeout;

                    else if (Exception is IOException && IsDiskFull())
                        Result.Status = StreamStatus.NoSpace;

                    else
                        Result.Status = StreamStatus.Broken;
                }
            }

            finally
            {
                if (Result.TraceId != Guid.Empty)
                    await Xnet.EmitAsync(Result);
            }
        }

        /// <summary>
        /// Tell the stream's cursor asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Tell"></param>
        /// <returns></returns>
        internal async Task TellAsync(Xnet Xnet, PKT_TELL Tell)
        {
            var Result = new PKT_SEEK_RESULT
            {
                Id = Tell.Id,
                TraceId = Tell.TraceId,
                Status = StreamStatus.NotImplemented
            };

            try
            {
                var Reg = m_Registry.Get(Tell.Id);
                if (Reg is null || Reg.Xnet != Xnet)
                {
                    Result.Status = StreamStatus.NotAStream;
                    return;
                }

                if (Reg.Stream.CanSeek == false)
                {
                    Result.Status = StreamStatus.NotSupported;
                    return;
                }

                try
                {
                    Result.Cursor = Reg.Stream.Position;
                    Result.Status = StreamStatus.Ok;
                }

                catch (Exception Exception)
                {
                    if (Exception is NotImplementedException)
                        Result.Status = StreamStatus.NotImplemented;

                    else if (Exception is NotSupportedException)
                        Result.Status = StreamStatus.NotSupported;

                    else if (Exception is InvalidOperationException)
                        Result.Status = StreamStatus.InvalidOperation;

                    else if (Exception is TimeoutException)
                        Result.Status = StreamStatus.Timeout;

                    else
                        Result.Status = StreamStatus.Broken;
                }
            }

            finally
            {
                if (Result.TraceId != Guid.Empty)
                    await Xnet.EmitAsync(Result);
            }
        }

        /// <summary>
        /// Write bytes to the stream asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Write"></param>
        /// <returns></returns>
        internal async Task WriteAsync(Xnet Xnet, PKT_WRITE Write)
        {
            var Result = new PKT_WRITE_RESULT
            {
                Id = Write.Id,
                TraceId = Write.TraceId,
                Status = StreamStatus.NotImplemented,
            };

            try
            {
                var Reg = m_Registry.Get(Write.Id);
                if (Reg is null || Reg.Xnet != Xnet)
                {
                    Result.Status = StreamStatus.NotAStream;
                    return;
                }

                if (Reg.Stream.CanWrite == false)
                {
                    Result.Status = StreamStatus.NotSupported;
                    return;
                }

                if (Write.Data is null || Write.Data.Length <= 0)
                {
                    Result.Status = StreamStatus.Ok;
                    return;
                }

                var Cursor = Reg.Stream.Position;

                try { await Reg.Stream.WriteAsync(Write.Data, Xnet.Closing); }
                catch(Exception Exception)
                {
                    if (Exception is OperationCanceledException Oce &&
                        Oce.CancellationToken == Xnet.Closing)
                    {
                        Result = null;
                        return;
                    }

                    if (Exception is NotImplementedException)
                        Result.Status = StreamStatus.NotImplemented;

                    else if (Exception is NotSupportedException)
                        Result.Status = StreamStatus.NotSupported;

                    else if (Exception is InvalidOperationException)
                        Result.Status = StreamStatus.InvalidOperation;

                    else if (Exception is TimeoutException)
                        Result.Status = StreamStatus.Timeout;

                    else if (Exception is IOException && IsDiskFull())
                        Result.Status = StreamStatus.NoSpace;

                    else
                        Result.Status = StreamStatus.Broken;
                }

                finally
                {
                    var Written = Reg.Stream.Position - Cursor;
                    if (Result != null && Written > 0)
                    {
                        Result.Size = (ushort)Written;
                        Result.Status = StreamStatus.Ok;
                    }
                }
            }

            finally
            {
                if (Result != null && Result.TraceId != Guid.Empty)
                    await Xnet.EmitAsync(Result);
            }
        }

        /// <summary>
        /// Flush the stream asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Flush"></param>
        /// <returns></returns>
        internal async Task FlushAsync(Xnet Xnet, PKT_FLUSH Flush)
        {
            var Result = new PKT_WRITE_RESULT
            {
                Id = Flush.Id,
                TraceId = Flush.TraceId,
                Status = StreamStatus.NotImplemented,
            };

            try
            {
                var Reg = m_Registry.Get(Flush.Id);
                if (Reg is null || Reg.Xnet != Xnet)
                {
                    Result.Status = StreamStatus.NotAStream;
                    return;
                }

                if (Reg.Stream.CanWrite == false)
                {
                    Result.Status = StreamStatus.NotSupported;
                    return;
                }

                Result.Status = StreamStatus.Ok;
                try { await Reg.Stream.FlushAsync(Xnet.Closing); }
                catch (Exception Exception)
                {
                    if (Exception is OperationCanceledException Oce && 
                        Oce.CancellationToken == Xnet.Closing)
                    {
                        Result = null;
                        return;
                    }

                    if (Exception is NotImplementedException)
                        Result.Status = StreamStatus.NotImplemented;

                    else if (Exception is NotSupportedException)
                        Result.Status = StreamStatus.NotSupported;

                    else if (Exception is InvalidOperationException)
                        Result.Status = StreamStatus.NotSupported;

                    else if (Exception is TimeoutException)
                        Result.Status = StreamStatus.Timeout;

                    else if (Exception is IOException && IsDiskFull())
                        Result.Status = StreamStatus.NoSpace;

                    else
                        Result.Status = StreamStatus.Broken;
                }
            }

            finally
            {
                if (Result != null && Result.TraceId != Guid.Empty)
                    await Xnet.EmitAsync(Result);
            }
        }

        /// <summary>
        /// Set the read-timeout of the stream asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="ReadTimeout"></param>
        /// <returns></returns>
        internal async Task SetReadTimeoutAsync(Xnet Xnet, PKT_READTIMEOUT ReadTimeout)
        {
            var Result = new PKT_READTIMEOUT_RESULT
            {
                Id = ReadTimeout.Id,
                TraceId = ReadTimeout.TraceId,
                Status = StreamStatus.NotImplemented,
            };

            try
            {
                var Reg = m_Registry.Get(ReadTimeout.Id);
                if (Reg is null || Reg.Xnet != Xnet)
                {
                    Result.Status = StreamStatus.NotAStream;
                    return;
                }

                if (Reg.Stream.CanTimeout == false)
                {
                    Result.Status = StreamStatus.NotSupported;
                    return;
                }

                Result.Status = StreamStatus.Ok;
                try { Reg.Stream.ReadTimeout = ReadTimeout.Timeout; }
                catch (Exception Exception)
                {
                    if (Exception is OperationCanceledException Oce &&
                        Oce.CancellationToken == Xnet.Closing)
                    {
                        Result = null;
                        return;
                    }

                    if (Exception is NotImplementedException)
                        Result.Status = StreamStatus.NotImplemented;

                    else if (Exception is InvalidOperationException)
                        Result.Status = StreamStatus.NotSupported;

                    else if (Exception is NotSupportedException)
                        Result.Status = StreamStatus.NotSupported;

                    else
                        Result.Status = StreamStatus.Broken;
                }

                try
                {
                    Result.Status = StreamStatus.Ok;
                    Result.Timeout = Reg.Stream.ReadTimeout;
                }
                catch
                {
                }
            }

            finally
            {
                if (Result != null && Result.TraceId != Guid.Empty)
                    await Xnet.EmitAsync(Result);
            }
        }

        /// <summary>
        /// Set the write-timeout of the stream asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="ReadTimeout"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal async Task SetWriteTimeoutAsync(Xnet Xnet, PKT_WRITETIMEOUT ReadTimeout)
        {
            var Result = new PKT_WRITETIMEOUT_RESULT
            {
                Id = ReadTimeout.Id,
                TraceId = ReadTimeout.TraceId,
                Status = StreamStatus.NotImplemented,
            };

            try
            {
                var Reg = m_Registry.Get(ReadTimeout.Id);
                if (Reg is null || Reg.Xnet != Xnet)
                {
                    Result.Status = StreamStatus.NotAStream;
                    return;
                }

                if (Reg.Stream.CanTimeout == false)
                {
                    Result.Status = StreamStatus.NotSupported;
                    return;
                }

                Result.Status = StreamStatus.Ok;
                try { Reg.Stream.WriteTimeout = ReadTimeout.Timeout; }
                catch (Exception Exception)
                {
                    if (Exception is OperationCanceledException Oce &&
                        Oce.CancellationToken == Xnet.Closing)
                    {
                        Result = null;
                        return;
                    }

                    if (Exception is NotImplementedException)
                        Result.Status = StreamStatus.NotImplemented;

                    else if (Exception is InvalidOperationException)
                        Result.Status = StreamStatus.NotSupported;

                    else if (Exception is NotSupportedException)
                        Result.Status = StreamStatus.NotSupported;

                    else
                        Result.Status = StreamStatus.Broken;
                }

                try
                {
                    Result.Status = StreamStatus.Ok;
                    Result.Timeout = Reg.Stream.WriteTimeout;
                }
                catch
                {
                }
            }

            finally
            {
                if (Result != null && Result.TraceId != Guid.Empty)
                    await Xnet.EmitAsync(Result);
            }
        }
    }
}
