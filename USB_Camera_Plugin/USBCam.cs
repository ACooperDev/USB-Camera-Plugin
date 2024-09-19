using System;
using Cognex.Designer.Core;
using Emgu.CV;
using Cognex.Designer.Components;
using Cognex.Designer.Core.Functions;
using Cognex.Designer.Scripting;
using Cognex.VisionPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        private bool _live = false;
        private int _camIndex;
        private Emgu.CV.Capture _capture;
        private CogImage24PlanarColor _resultImage;

        private const string ImageResultEventID = "ReadImageEvent";
        private readonly ScriptablePoint _readImageEvent;

        private const string ConnectedEventID = "ConnectedEvent";
        private readonly ScriptablePoint _connectedEvent;

        private const string DisconnectedEventID = "DisconnectedEvent";
        private readonly ScriptablePoint _disconnectedEvent;

        private const string GetCapPropEventID = "GetCapPropEvent";
        private readonly ScriptablePoint _getCapPropEvent;

        //capPropDictionary[3].Description
        private Dictionary<int,(string Key, string Description)> capPropDictionary = new Dictionary<int, (string Key, string Description)>
        {
            {-4, ("DC1394Off", "Turn the feature off (not controlled manually nor automatically)")},
            {-3, ("DC1394ModeManual", "Set automatically when a value of the feature is set by the user")},
            {-2, ("DC1394ModeAuto", "DC1394 mode auto")},
            {-1, ("DC1394ModeOnePushAuto", "DC1394 mode one push auto")},
            {0, ("PosMsec", "Film current position in milliseconds or video capture timestamp")},
            {1, ("PosFrames", "0-based index of the frame to be decoded/captured next")},
            {2, ("PosAviRatio", "Position in relative units (0 - start of the file, 1 - end of the file)")},
            {3, ("FrameWidth", "Width of frames in the video stream")},
            {4, ("FrameHeight", "Height of frames in the video stream")},
            {5, ("Fps", "Frame rate")},
            {6, ("FourCC", "4-character code of codec")},
            {7, ("FrameCount", "Number of frames in video file")},
            {8, ("Format", "Format")},
            {9, ("Mode", "Mode")},
            {10, ("Brightness", "Brightness")},
            {11, ("Contrast", "Contrast")},
            {12, ("Saturation", "Saturation")},
            {13, ("Hue", "Hue")},
            {14, ("Gain", "Gain")},
            {15, ("Exposure", "Exposure")},
            {16, ("ConvertRgb", "Convert RGB")},
            {17, ("WhiteBalanceBlueU", "White balance blue u")},
            {18, ("Rectification", "Rectification")},
            {19, ("Monochrome", "Monochrome")},
            {20, ("Sharpness", "Sharpness")},
            {21, ("AutoExposure", "Exposure control done by camera, user can adjust reference level using this feature")},
            {22, ("Gamma", "Gamma")},
            {23, ("Temperature", "Temperature")},
            {24, ("Trigger", "Trigger")},
            {25, ("TriggerDelay", "Trigger delay")},
            {26, ("WhiteBalanceRedV", "White balance red v")},
            {27, ("Zoom", "Zoom")},
            {28, ("Focus", "Focus")},
            {29, ("Guid", "GUID")},
            {30, ("IsoSpeed", "ISO SPEED")},
            {31, ("MaxDC1394", "MAX DC1394")},
            {32, ("Backlight", "Backlight")},
            {33, ("Pan", "Pan")},
            {34, ("Tilt", "Tilt")},
            {35, ("Roll", "Roll")},
            {36, ("Iris", "Iris")},
            {37, ("Settings", "Settings")},
            {1024, ("Autograb", "property for highgui class CvCapture_Android only")},
            {1025, ("SupportedPreviewSizesString", "readonly, tricky property, returns const char* indeed")},
            {1026, ("PreviewFormat", "readonly, tricky property, returns const char* indeed")},
            {-2147483648, ("OpenniDepthGenerator", "OpenNI map generators")},
            {1073741824, ("OpenniImageGenerator", "OpenNI map generators")},
            {-1073741824, ("OpenniGeneratorsMask", "OpenNI map generators")},
            {100, ("OpenniOutputMode", "Properties of cameras available through OpenNI interfaces")},
            {101, ("OpenniFrameMaxDepth", "Properties of cameras available through OpenNI interfaces, in mm.")},
            {102, ("OpenniBaseline", "Properties of cameras available through OpenNI interfaces, in mm.")},
            {103, ("OpenniFocalLength", "Properties of cameras available through OpenNI interfaces, in pixels.")},
            {104, ("OpenniRegistration", "Flag that synchronizes the remapping depth map to image map by changing depth generator's view point (if the flag is \"on\") or sets this view point to its normal one (if the flag is \"off\").")},
            {105, ("OpenniApproxFrameSync", "Approx frame sync")},
            {106, ("OpenniMaxBufferSize", "Max buffer size")},
            {107, ("OpenniCircleBuffer", "Circle buffer")},
            {108, ("OpenniMaxTimeDuration", "Max time duration")},
            {109, ("OpenniGeneratorPresent", "Generator present")},
            {1073741933, ("OpenniImageGeneratorPresent", "Openni image generator present")},
            {1073741924, ("OpenniImageGeneratorOutputMode", "Image generator output mode")},
            {-2147483546, ("OpenniDepthGeneratorBaseline", "Depth generator baseline, in mm.")},
            {-2147483545, ("OpenniDepthGeneratorFocalLength", "Depth generator focal length, in pixels.")},
            {-2147483544, ("OpenniDepthGeneratorRegistration", "Openni generator registration")},
            {200, ("GstreamerQueueLength", "Properties of cameras available through GStreamer interface. Default is 1")},
            {300, ("PvapiMulticastip", "Ip for enable multicast master mode. 0 for disable multicast")},
            {400, ("XiDownsampling", "Change image resolution by binning or skipping.")},
            {401, ("XiDataFormat", "Output data format")},
            {402, ("XiOffsetX", "Horizontal offset from the origin to the area of interest (in pixels).")},
            {403, ("XiOffsetY", "Vertical offset from the origin to the area of interest (in pixels).")},
            {404, ("XiTrgSource", "Defines source of trigger.")},
            {405, ("XiTrgSoftware", "Generates an internal trigger. PRM_TRG_SOURCE must be set to TRG_SOFTWARE.")},
            {406, ("XiGpiSelector", "Selects general purpose input")},
            {407, ("XiGpiMode", "Set general purpose input mode")},
            {408, ("XiGpiLevel", "Get general purpose level")},
            {409, ("XiGpoSelector", "Selects general purpose output")},
            {410, ("XiGpoMode", "Set general purpose output mode")},
            {411, ("XiLedSelector", "Selects camera signaling LED")},
            {412, ("XiLedMode", "Define camera signaling LED functionality")},
            {413, ("XiManualWb", "Calculates White Balance(must be called during acquisition)")},
            {414, ("XiAutoWb", "Automatic white balance")},
            {415, ("XiAeag", "Automatic exposure/gain")},
            {416, ("XiExpPriority", "Exposure priority (0.5 - exposure 50%, gain 50%).")},
            {417, ("XiAeMaxLimit", "Maximum limit of exposure in AEAG procedure")},
            {418, ("XiAgMaxLimit", "Maximum limit of gain in AEAG procedure")},
            {419, ("XiAeagLevel", "Average intensity of output signal AEAG should achieve(in %)")},
            {420, ("XiTimeout", "Image capture timeout in milliseconds")},
            {8001, ("AndroidFlashMode", "Android flash mode")},
            {8002, ("AndroidFocusMode", "Android focus mode")},
            {8003, ("AndroidWhiteBalance", "Android white balance")},
            {8004, ("AndroidAntibanding", "Android anti banding")},
            {8005, ("AndroidFocalLength", "Android focal length")},
            {8006, ("AndroidFocusDistanceNear", "Android focus distance near")},
            {8007, ("AndroidFocusDistanceOptimal", "Android focus distance optimal")},
            {8008, ("AndroidFocusDistanceFar", "Android focus distance far")},
            {9001, ("IOSDeviceFocus", "iOS device focus")},
            {9002, ("IOSDeviceExposure", "iOS device exposure")},
            {9003, ("IOSDeviceFlash", "iOS device flash")},
            {9004, ("IOSDeviceWhitebalance", "iOS device white-balance")},
            {9005, ("IOSDeviceTorch", "iOS device torch")},
            {10001, ("GigaFrameOffsetX", "Smartek Giganetix Ethernet Vision: frame offset X")},
            {10002, ("GigaFrameOffsetY", "Smartek Giganetix Ethernet Vision: frame offset Y")},
            {10003, ("GigaFrameWidthMax", "Smartek Giganetix Ethernet Vision: frame width max")},
            {10004, ("GigaFrameHeighMax", "Smartek Giganetix Ethernet Vision: frame height max")},
            {10005, ("GigaFrameSensWidth", "Smartek Giganetix Ethernet Vision: frame sens width")},
            {10006, ("GigaFrameSensHeigh", "Smartek Giganetix Ethernet Vision: frame sens height")}
        };

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

        [Published]
        public void FireSetCapProp()
        {
            _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Settings, 1);
        }

        [Published]
        public void FireLiveMode(bool live)
        {
            _live = live;
        }

        //Emgu.CV event called when an image is grabbed.
        private void _capture_ImageGrabbed(object sender, EventArgs e)
        {            
            try
            {
                //Stop the capture
                if (!_live)
                {
                    _capture.Stop();
                }

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
