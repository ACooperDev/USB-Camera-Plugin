using System;
using Cognex.Designer.Core;
using Emgu.CV;
using Cognex.Designer.Components;
using Cognex.Designer.Core.Functions;
using Cognex.Designer.Scripting;
using Cognex.VisionPro;
using System.Collections.Generic;
using Emgu.CV.CvEnum;

//add a FireErrorOccured for anytime a try catch block happens.

//Scriptable component for USB cameras based on EMGU.CV 3.1.0.1
//https://www.nuget.org/packages/EmguCV/3.1.0.1

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
        //Global variables.
        private readonly ScriptablePoint[] _scriptPoints;

        private bool _live = false;
        private int _camIndex;
        private Emgu.CV.Capture _capture;
        private CogImage24PlanarColor _resultImage;
        private List<double> capPropValues;

        private const string ImageResultEventID = "ReadImageEvent";
        private readonly ScriptablePoint _readImageEvent;

        private const string ConnectedEventID = "ConnectedEvent";
        private readonly ScriptablePoint _connectedEvent;

        private const string DisconnectedEventID = "DisconnectedEvent";
        private readonly ScriptablePoint _disconnectedEvent;

        private const string GetCapPropEventID = "GetCapPropEvent";
        private readonly ScriptablePoint _getCapPropEvent;

        private const string GetErrorEventID = "GetErrorEvent";
        private readonly ScriptablePoint _getErrorEvent;

        //Inherited method.
        protected override ScriptablePoint[] GetScriptPoints()
        {
            return _scriptPoints;
        }
        
        //Initialize variables and create scriptable components.
        public USBCam()
        {
            capPropValues = new List<double>();

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

            _getCapPropEvent = new ScriptablePoint(
                GetCapPropEventID,
                "Get Capture Property",
                "Some description",
                    runParameters: new[]
                    {
                        new ArgumentDescriptor("Get CapProp", typeof(string)),
                    },
                    returnType: typeof(void));

            _getErrorEvent = new ScriptablePoint(
                GetErrorEventID,
                "Get Error Property",
                "Some description",
                    runParameters: new[]
                    {
                        new ArgumentDescriptor("Get Error", typeof(string)),
                    },
                    returnType: typeof(void));

            //Create scriptable points array to make events accesable in Designer.
            _scriptPoints = new[] {_readImageEvent, _connectedEvent, _disconnectedEvent, _getCapPropEvent, _getErrorEvent};
        }

        //Published
        //Saved parameters for the component.
        [Published]
        [Saved]
        public int camIndex
        {
            get => _camIndex;
            set => SetBackingField(ref _camIndex, value);
        }

        //$Functions for component.
        //Connect to a camera, subscribe to the image grabbed event and run a scriptable event.
        [Published]
        public void FireConnectEvent()
        {
            try
            {
                if (_capture != null)
                {
                    _capture.Stop();
                    _capture.Dispose();
                    _capture = null;
                }
            }catch(Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }

            try
            {
                _capture = new Emgu.CV.Capture(_camIndex);
                _capture.ImageGrabbed += _capture_ImageGrabbed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }

            RunScript(ConnectedEventID);
        }
      
        //Disconnect from a camera and run a scriptable event.
        [Published]
        public void FireDisconnectEvent()
        {
            try
            {
                _capture.ImageGrabbed -= _capture_ImageGrabbed;
                _capture.Stop();
                _capture.Dispose();
                _capture = null;

                RunScript(DisconnectedEventID);
            }catch(Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }
        }

        //Trigger a camera which is tied to the subscribed event _capture_ImageGrabbed.
        [Published]
        public void FireTriggerEvent()
        {
            try
            {
                _capture.Start();
            } catch(Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }
        }

        //Popup a UI wizard for setting capture properties.
        [Published]
        public void FireCapPropWizard()
        {
            try
            {
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Settings, 1);
            }catch(Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }
        }

        //Set an individual capture property.
        [Published]
        public void FireSetCapProp(Emgu.CV.CvEnum.CapProp capProp, double value)
        {
            try
            {
                _capture.SetCaptureProperty(capProp, value);
            }catch(Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }
        }

        //Get an individual capture property.
        [Published]
        public double FireGetCapProp(Emgu.CV.CvEnum.CapProp capProp)
        {
            double capPropValue = 0;

            try
            {
               capPropValue = _capture.GetCaptureProperty(capProp);
                RunScript(GetCapPropEventID);
            }catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }

            return capPropValue;
        }

        //Set the live mode logic.
        [Published]
        public void FireLiveMode(bool live)
        {
            _live = live;
        }

        //Clear capProValues list and save a copy of the current capture properties to the list.
        [Published]
        public List<double> FireSaveCapProp()
        {
            try
            {
                capPropValues.Clear();
            }catch(Exception ex) {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            };

            try
            {
                foreach (CapProp prop in Enum.GetValues(typeof(CapProp)))
                {
                    capPropValues.Add(_capture.GetCaptureProperty(prop));
                }
            }catch(Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }

            return capPropValues;
        }

        ///Load a list of capture properties to the camera
        [Published]
        public void FireLoadCapProp(List<double> capPropList)
        {
            int index = 0;

            try
            {
                foreach (CapProp prop in Enum.GetValues(typeof(CapProp)))
                {
                    if (prop != Emgu.CV.CvEnum.CapProp.Settings)
                    {
                        _capture.SetCaptureProperty(prop, capPropList[index]);
                    }
                    index++;
                }
            } catch(Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }
        }

        //Emgu.CV event called when an image is grabbed.  Retrieve the image and convert it to a VPro image type.
        private void _capture_ImageGrabbed(object sender, EventArgs e)
        {            
            try
            {
                if(!_live)
                {
                    _capture.Stop();
                }

                using(Mat m = new Mat())
                {
                    _capture.Retrieve(m); _capture.Retrieve(m);

                    using(System.Drawing.Bitmap myBitmapColor = m.Bitmap)
                    {
                        _resultImage = new Cognex.VisionPro.CogImage24PlanarColor(myBitmapColor);

                        RunScript(ImageResultEventID, _resultImage);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                RunScript(GetErrorEventID, ex.Message);
            }
        }
    } 
}
