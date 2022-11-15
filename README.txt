This tool is provided free of charge for non-profit organizations or vendors supporting non-profits. Please feel free to copy the source and use it in any projects you see fit. 

Keep improving the world and changing lives. 

1) INTRO
This tool allows you to download your constituent attachments in bulk from Blackbaud's Raiser's Edge NXT. 

2) PREREQS
This application uses Blackbaud's SKY API to get the required information at runtime. 
In order to run the application successfully, you will need to do the following:
	
	1) Create a Blackbaud Developer account 
	2) Subscribe to their API service (a free account is fine, but will significantly increase the runtime, as you are limited to 25,000 calls in a 24HR period at time of writing)
		2.a) These instructions should help out: https://developer.blackbaud.com/skyapi/docs/applications/createapp
	3) Create an application 
		3.a) Use http://localhost:5000 as your redirect_uri. Otherwise, the application will fail its initial auth request. 
	4) Link the application to your Blackbaud Environment


3) SETUP
Once you've completed the tasks in section 2, you will need to load the information into the config.json file. The application will handle the rest of the work.
	1) Load the client_id, client_secret, and your BB Dev Key to the config.Template.json file, and rename it to config.json in the same location as the compiled executable. 

4) RUNNING THE APPLICATION
The program loads the configuration from the config file, gets required authentication information, and then does the following:

	1) Creates a constituent list as a json file. 
	2) Parses the constituent list for attachments.
	3) Downloads the attachments in an application subfolder. 

If your access token expires, the application should automatically refresh. If you run out of API calls, the application will wait until it can successfully complete calls again. 

The application will automatically update your constituent list per run, so if you need to quit, the application should be able to pick up where you left off at any of the above steps. 

5) TROUBLESHOOTING
If the application fails immediately after running, there's probably something wrong with the config file. Make sure text is surrounded by quotes.

Most issues can be cleared up by either deleting the constituentList.json file, or the progress.json file. This will remove the app's ability to restart where you left off, but it will fix most common issues. 

6) UPDATES
Our organization is moving away from Blackbaud products soon. So this codebase can be considered abandoned past 2024. 
