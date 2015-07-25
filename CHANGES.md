2.0.49
 * Fixed typo: ModifyConnectionSetttings to ModifyConnectionSettings.

2.0.42
 * Added an overload so the AppSettings reader can be used to configure the ES sink. 

2.0.38
 * Fixes an issue where the index decider was not properly used with pusing events thorugh the ElasticLogShipper.

2.0.37
 * When auto register of the template is enabled, but the ES server is unavailable, the exception is logged to the selflog instead of bubbling up the exception.
 * omit_terms is set to true in the template.

2.0.0
 * Moved the Elasticsearch sink from its [original location](https://github.com/serilog/serilog)
