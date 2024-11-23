using System;

namespace ScriptingForm.Scripts
{
    public interface IRealtimeDataProvider
    {
        double GetValueByName(string inputName);
        double GetTargetByName(string inputName);
        string GetUnit(string inputName);
        bool HasChannel(string inputName);
    }
}