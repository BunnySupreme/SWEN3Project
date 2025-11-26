using log4net;
using log4net.Core;

public sealed class FakeLogger : ILog
{
    public bool IsDebugEnabled => false;
    public bool IsInfoEnabled => false;
    public bool IsWarnEnabled => false;
    public bool IsErrorEnabled => false;
    public bool IsFatalEnabled => false;

    public ILogger Logger => throw new NotImplementedException();

    public void Debug(object message) { }
    public void Debug(object message, Exception exception) { }

    public void DebugFormat(string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }

    public void DebugFormat(string format, object? arg0)
    {
        throw new NotImplementedException();
    }

    public void DebugFormat(string format, object? arg0, object? arg1)
    {
        throw new NotImplementedException();
    }

    public void DebugFormat(string format, object? arg0, object? arg1, object? arg2)
    {
        throw new NotImplementedException();
    }

    public void DebugFormat(IFormatProvider? provider, string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }

    public void Error(object message) { }
    public void Error(object message, Exception exception) { }

    public void ErrorFormat(string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }

    public void ErrorFormat(string format, object? arg0)
    {
        throw new NotImplementedException();
    }

    public void ErrorFormat(string format, object? arg0, object? arg1)
    {
        throw new NotImplementedException();
    }

    public void ErrorFormat(string format, object? arg0, object? arg1, object? arg2)
    {
        throw new NotImplementedException();
    }

    public void ErrorFormat(IFormatProvider? provider, string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }

    public void Fatal(object message) { }
    public void Fatal(object message, Exception exception) { }

    public void FatalFormat(string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }

    public void FatalFormat(string format, object? arg0)
    {
        throw new NotImplementedException();
    }

    public void FatalFormat(string format, object? arg0, object? arg1)
    {
        throw new NotImplementedException();
    }

    public void FatalFormat(string format, object? arg0, object? arg1, object? arg2)
    {
        throw new NotImplementedException();
    }

    public void FatalFormat(IFormatProvider? provider, string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }

    public void Info(object message) { }
    public void Info(object message, Exception exception) { }

    public void InfoFormat(string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }

    public void InfoFormat(string format, object? arg0)
    {
        throw new NotImplementedException();
    }

    public void InfoFormat(string format, object? arg0, object? arg1)
    {
        throw new NotImplementedException();
    }

    public void InfoFormat(string format, object? arg0, object? arg1, object? arg2)
    {
        throw new NotImplementedException();
    }

    public void InfoFormat(IFormatProvider? provider, string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }

    public void Warn(object message) { }
    public void Warn(object message, Exception exception) { }

    public void WarnFormat(string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }

    public void WarnFormat(string format, object? arg0)
    {
        throw new NotImplementedException();
    }

    public void WarnFormat(string format, object? arg0, object? arg1)
    {
        throw new NotImplementedException();
    }

    public void WarnFormat(string format, object? arg0, object? arg1, object? arg2)
    {
        throw new NotImplementedException();
    }

    public void WarnFormat(IFormatProvider? provider, string format, params object?[]? args)
    {
        throw new NotImplementedException();
    }
}
