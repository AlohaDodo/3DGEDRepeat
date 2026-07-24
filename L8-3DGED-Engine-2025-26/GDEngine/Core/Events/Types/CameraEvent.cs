namespace GDEngine.Core.Events
{
    public class CameraEvent
    {
        private string targetCameraName;

        public CameraEvent(string targetCameraName)
        {
            this.targetCameraName = targetCameraName;
        }

        public string TargetCameraName { get => targetCameraName; set => targetCameraName = value; }
    }
}
