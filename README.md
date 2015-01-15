# CI
Continuous integration with TFS: plugin, build definitions, nuget packaging, repository, unit tests, deployment, etc

Goal

We want to ensure code can only be checked in when the following criteria are met:

1)	Code review: code need to be reviewed by peers
2)	Styles must satisfy predefined rules (i.e. FxCop)
3)	Unit tests: affected unit tests should still pass
4)	Code coverage: code coverage need to be above predefined percentage

Build Process

When gated checkin is enabled in build definition, TFS automatically creates shelveset and ensure solution is successfully compiled and all tests are passed before shelveset can be checked in. However, TFS do not enforce code style rules and code coverage rules. In order to enforce code style rules and code coverage rules are applied, TFS build needs to be customized. 

Code Review

TFS supports code review by creating two work items (code review request, code review response). Code review process (out of the box) is optional and cannot be easily changed within TFS. Another limitation is that TFS does not provide customization of code review policy, for example, if we want to enable certain rules for code review process (i.e. committer cannot review his/her own code; only a few chosen peers can be code reviewers, etc), we need to write plugin code and deploy it to TFS.

Acknowledgement

Associated code are downloaded and modified from the following resources:
•	TFS Plugins (http://tfspluginsuite.codeplex.com)
•	TFS Build Extensions (http://tfsbuildextensions.codeplex.com)
