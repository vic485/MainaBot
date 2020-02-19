# WebHooks
* Change namespace WebHooks into HTTPServer.
* Change WebHooksManager into HTTPServerManager.
* Change WebHookListener into HTTPServer.
* Separate request handling into prefixes:
	* **/webhooks/** -> GitHubWebHooks POST requests
	* **/rss/** -> RSS Subscription GET and POST requests
	* **/embed/** -> Discord Embed POST requests
* Make an abstract class RequestHandler to handle HTTP requests.

  The objective is to have one class per prefix which handles all requests made to that prefix.
* Setup HTTPServer to send requests to the appropiate RequestHandler based on prefix.
* Setup HTTPServer to use events to notify the HTTPServerManager of actions needed to take due to requests.

  These events pass the RequestHandler as args.
* HTTPServerManager then use the state of the RequestHandler to take appropiate action.
* Based on the result of the action, use the RequestHandler to send the appropiate response.
* Substitute the WebHookIntermediary interface for the HTTPRequestEvent.

# RSS
* Add error events to RSSClient.
* Handle RSSClient error events in RSSManager
