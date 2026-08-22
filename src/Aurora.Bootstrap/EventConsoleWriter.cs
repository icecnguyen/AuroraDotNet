using System;
using System.IO;
using System.Text;

#pragma warning disable CA1515
#pragma warning disable CA1003
namespace Aurora.Bootstrap;

public class EventConsoleWriter : TextWriter
{
    private readonly TextWriter _originalOut;
    public event Action<string>? OnLog;

    public EventConsoleWriter(TextWriter originalOut)
    {
        _originalOut = originalOut;
    }

    public override Encoding Encoding => _originalOut.Encoding;

    public override void Write(char value)
    {
        _originalOut.Write(value);
    }

    public override void WriteLine(string? value)
    {
        _originalOut.WriteLine(value);
        if (value != null)
        {
            OnLog?.Invoke(value);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _originalOut.Dispose();
        }
        base.Dispose(disposing);
    }
}
#pragma warning restore CA1515
#pragma warning restore CA1003
