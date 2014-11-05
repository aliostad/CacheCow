Feature: ExceptionHandling
	In order to prevent rendering server useless due to cache store exceptions
	As a CacheCow.Server client
	I want to be able to carry on using server even with faulty caching 

Scenario Outline: Ignore Exception Policy
	Given I have an API running CacheCow Server and using <EntityTagStore> storage 
	And my error policy is set to ignore
	When I create a new item
	Then Get a successful response

Examples: 
| EntityTagStore |
| InMemoryFaulty |

Scenario Outline: Default Exception Policy
	Given I have an API running CacheCow Server and using <EntityTagStore> storage 
	When I create a new item
	Then Get an unsuccessful response

Examples: 
| EntityTagStore |
| InMemoryFaulty |