OVERVIEW
================

Ubi Displays Beta
John Hardy
john@highwire-dtc.com
HighWire Programme, Lancaster University
22-11-2012

QUICK GUIDE
================
So you want to get started?

1) Download and install the Kinect SDK v1.6 ( http://www.microsoft.com/en-us/kinectforwindows/develop/developer-downloads.aspx )
2) Run Ubi Displays
3) Follow the on-screen steps
4) Drag HTML files onto surfaces to deploy displays


INTRODUCTION
================
Ubi Displays simplifies the process of creating interactive projected displays using a projector and a Microsoft Kinect.  Using the interface it is possible to create interactive displays on almost any static, flat object: walls, floors, books, beds, etc.  

Check out the video here: http://www.youtube.com/watch?v=df1NO7MoAUY

The displays themselves use HTML, CSS and JavaScript to display content.  Using the provided API, you can write JavaScript logic which can make displays appear, disappear, move, change interaction modality to suit the content (i.e. swap multi-touch for a foot detector) depending on physical factors and the kind of content it is trying to display.

Ubi Displays is a research project.  Although the hardware involved is relatively expensive at the moment (the price point often outweighs the value of the displays that can be created), researchers predict that wireless, steerable pico-projectors will soon be a commercially viable replacement for the lightbulb.  If that happens - my research aims to answer the question of: what will we do with it?  By letting everyone experiment with the technology, I hope to find out what kinds of things this technology is good at, what it is bad at, and how we can design better computer interfaces in the future. 

Any and all feedback is more than welcome!  Ideas, suggestions, interesting things you have noticed with how people react to the technology and drawbacks of the whole idea can be emailed directly to me!  Although please send bug reports to Google Code: (http://code.google.com/p/ubidisplays/issues/list).

There is also an accompanying academic paper, which you can download here: http://highwire-dtc.com/url/ubidisplayspaper


HARDWARE REQUIREMENTS
================
(1) Microsoft XBox (or Windows) Kinect
(2) Projector
(3) Windows 7 (or higher) PC with i5 Processor or Higher



SOFTWARE REQUIREMENTS
================
(1) Kinect SDK 1.6: 	http://www.microsoft.com/en-us/kinectforwindows/develop/developer-downloads.aspx


NOTES
================
Still reading?  Awesome.  If you feel like geeking out, you can read the paper on this toolkit here: http://highwire-dtc.com/url/ubidisplayspaper.  It has some interesting graphs which describe the accuracy etc.


ACKNOWLEDGEMENTS
================

The following are 3rd party libraries or code which is integrated into this project.

[Kinect for Windows SDK]	http://www.microsoft.com/en-us/kinectforwindows/
[SlimMath]			http://code.google.com/p/slimmath/
[Awesomium]	*		http://awesomium.com

* As this project has commercial restrictions on the Awesomium web library, you will need to buy a license if you wish to profit from Ubi Displays and make over a certain amount of money.  If you have read their terms and conditions and are in and doubt, please email me for more information.


The following are resources and public domain code snippts which are were useful in creating this software.

[Point in Polygon Article]	http://alienryderflex.com/polygon/
[Running Average]		http://www.johndcook.com/standard_deviation.html
[Johnny Lee Warper.cs]		http://johnnylee.net/projects/wii/
[Rotating Calipers]		http://cgm.cs.mcgill.ca/~orm/maer.html and http://www.vb-helper.com/howto_net_find_bounding_rectangle.html
[Graham Scan] 			http://softsurfer.com/Archive/algorithm_0109/algorithm_0109.htm
[Icons]				http://www.famfamfam.com/
[WPF Glass]			http://msdn.microsoft.com/en-us/library/ms748975.aspx
[Best Fit Plane]		http://codesuppository.blogspot.com/2006/03/best-fit-plane.html
