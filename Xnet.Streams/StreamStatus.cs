namespace XnetStreams
{
    /// <summary>
    /// Stream Status.
    /// </summary>
    public enum StreamStatus : byte
    {
        /// <summary>
        /// Ok.
        /// </summary>
        Ok = 0,

        /// <summary>
        /// Unauthorized access.
        /// </summary>
        Unauthorized,

        /// <summary>
        /// Forbidden access.
        /// </summary>
        Forbidden,

        /// <summary>
        /// Not supported operation.
        /// </summary>
        NotSupported,

        /// <summary>
        /// Not implemented operation.
        /// </summary>
        NotImplemented,

        /// <summary>
        /// Unhandled exception occured.
        /// </summary>
        UnhandledException,

        /// <summary>
        /// Specified identification is not a stream.
        /// </summary>
        NotAStream,

        /// <summary>
        /// Invalid operation.
        /// </summary>
        InvalidOperation,

        /// <summary>
        /// Target identification is conflict.
        /// </summary>
        Conflict,

        /// <summary>
        /// Target is busy.
        /// </summary>
        Busy,

        /// <summary>
        /// Timeout reached.
        /// </summary>
        Timeout,

        /// <summary>
        /// Stream is broken.
        /// </summary>
        Broken,

        /// <summary>
        /// Already exists.
        /// </summary>
        AlreadyExists,

        /// <summary>
        /// Path not found.
        /// </summary>
        PathNotFound,

        /// <summary>
        /// Path name is too long.
        /// </summary>
        PathTooLong,

        /// <summary>
        /// Target is exclusive resource, Sharing violation.
        /// </summary>
        Exclusive,

        /// <summary>
        /// No space to accept more bytes.
        /// </summary>
        NoSpace,

        /// <summary>
        /// Cursor reached on end of the stream.
        /// </summary>
        EndOfStream,

        /// <summary>
        /// Target is read-only.
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Target is write-only.
        /// </summary>
        WriteOnly,

        /// <summary>
        /// Target can not be invoke an action with specified parameters.
        /// </summary>
        InvalidParameters
    }
}
