using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptingForm.Scripts
{
    public interface IEziioController
    {
        void SetOutputByName(string name);
        void ClearOutputByName(string name);
    }

    public interface ISlidesController
    {
        Task ActivateSlideAsync(string slideName);
        Task DeactivateSlideAsync(string slideName);
    }

    public interface IGraphManager
    {
        Task MoveToPoint(string pointName, bool showDialog);
    }

    public interface ICountdownPopup
    {
        Task ShowCountdownAsync(int milliseconds);
        Task ShowCountdownAsync(int milliseconds, Action completionCallback);
    }
}
