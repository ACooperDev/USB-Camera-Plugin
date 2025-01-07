# USB-Camera-Plugin

A Designer Component Plugin for interacting with generic USB cameras.

Built using EmguCV:
- https://www.nuget.org/packages/EmguCV/3.1.0.1

## Definitions

Scriptable Events
- Read Image Event
- Connected Event
- Disconnected Event
- Get Error Property

$ Functions
- camIndex
	- int
	- Get/set the index of the USB camera.
- FireCapPropWizard()
	- Returns void
	- Loads a UI wizard for setting USB camera properties.
- FireConnectEvent()
	- Returns void
	- Connects to a USB camera.
- FireDisconnectEvent()
	- Returns void
	- Disconnects from a USB camera.
- FireGetCapProp(Emgu.CV.CVEnum.CapProp CapProp)
	- Returns double
	- The enumerable value representing a USB camera capture property.
- FireLiveMode(bool live)
	- Returns void
	- Enables/disables USB camera live mode.
- FireLoadCapProp
- FireSaveCapProp
- FireSetCapProp(Emgu.CV.CVEnum.CapProp CapProp, double value)
	- Returns void
	- Sets the value of a Emgu.CV.CVEnum.CapProp.
- FireTriggerEvent