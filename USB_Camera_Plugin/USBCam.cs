using System;
using Cognex.Designer.Core;
using Emgu.CV;
using Cognex.Designer.Components;
using Cognex.Designer.Core.Functions;
using Cognex.Designer.Scripting;
using Cognex.VisionPro;

//Scriptable component for USB cameras based on EMGU.CV 3.1.0.1
//https://github.com/emgucv/emgucv/releases/tag/4.9.0

namespace USB_Camera_Plugin
{
    //The category in the Component Browser when a new component is created.
    [Category("USB_Cameras")]
    //The name displayed under the Category.
    [DisplayName("USB Cameras")]
    //The description of the component.
    [Description("A component for capturing images from a USB camera.")]
    //Expose a property to the new component dialog aka Parameter Configuration window.
    [Parameter(1, nameof(camIndex), "Camera Index", typeof(int), null)]
    //The tpye ID used during save. Allows you to refactor the code later while not breaking serialization.
    [TypeName("ScriptableComponent")]
    public class USBCam : UserComponentBase
    {
        private readonly ScriptablePoint[] _scriptPoints;
        private int _camIndex;
        private Emgu.CV.Capture _capture;
        private CogImage24PlanarColor _resultImage;

        private const string ImageResultEventID = "ReadImageEvent";
        private readonly ScriptablePoint _readImageEvent;

        private const string ConnectedEventID = "ConnectedEvent";
        private readonly ScriptablePoint _connectedEvent;

        private const string DisconnectedEventID = "DisconnectedEvent";
        private readonly ScriptablePoint _disconnectedEvent;


        //Inherited method.
        protected override ScriptablePoint[] GetScriptPoints()
        {
            return _scriptPoints;
        }
        
         
        //Events for the component.
        public USBCam()
        {
            _readImageEvent = new ScriptablePoint(
                ImageResultEventID,
                "Read Image Event",
                "Some description",
                    runParameters: new[]
                        {
                        new ArgumentDescriptor("ImageResult", typeof(CogImage24PlanarColor)),
                        },
                returnType: typeof(void));

            _connectedEvent = new ScriptablePoint(ConnectedEventID,"Connected Event","Some description");

            _disconnectedEvent = new ScriptablePoint(DisconnectedEventID, "Disconnected Event", "Some description");

            //Create scriptable points array to make events accesable in Designer.
            _scriptPoints = new[] {_readImageEvent, _connectedEvent, _disconnectedEvent};

        }

        //Saved parameters.
        //Published
        [Published]
        [Saved]
        public int camIndex
        {
            get => _camIndex;
            set => SetBackingField(ref _camIndex, value);
        }

        //$Functions for component.
        [Published]
        public void FireConnectEvent()
        {
            //Dispose of existing capture if necessary
            if (_capture != null)
            {
                _capture.Stop();
                _capture.Dispose();
                _capture = null;
            }
            _capture = new Emgu.CV.Capture(_camIndex);

            _capture.ImageGrabbed += _capture_ImageGrabbed;

            RunScript(ConnectedEventID);
        }
      
        [Published]
        public void FireDisconnectEvent()
        {
            _capture.ImageGrabbed -= _capture_ImageGrabbed;
            _capture.Stop();
            _capture.Dispose();
            _capture = null;
            RunScript(DisconnectedEventID);
        }

        [Published]
        public void FireTriggerEvent()
        {
            _capture.Start();
        }


        //Emgu.CV event called when an image is grabbed.
        private void _capture_ImageGrabbed(object sender, EventArgs e)
        {            
            try
            {
                //Stop the capture
                _capture.Stop();

                //Initialize a new Mat object
                using (Mat m = new Mat())
                {
                    //Retrieve the current frame
                    _capture.Retrieve(m); _capture.Retrieve(m);

                    //Convert Mat to Bitmap
                    using (System.Drawing.Bitmap myBitmapColor = m.Bitmap)
                    {
                        //Convert to VPro object.
                        _resultImage = new Cognex.VisionPro.CogImage24PlanarColor(myBitmapColor);

                        //Run scriptable event.
                        RunScript(ImageResultEventID, _resultImage);
                    }
                }
            }
            catch (Exception ex)
            {
                //Handle the exception (e.g., log the error or show a message)
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

        }
    }
    
}
