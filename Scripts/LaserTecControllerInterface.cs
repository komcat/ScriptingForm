using System.Threading.Tasks;


namespace ScriptingForm.Scripts
{
    // Interface for LaserTECController operations
    public interface ILaserTECController
    {
        Task SetLowCurrent();
        Task SetHighCurrent();
        Task TurnOnTEC();
        Task TurnOffTEC();
        Task TurnOnLaser();
        Task TurnOffLaser();
    }

    
}