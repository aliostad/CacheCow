Feature: CacheInvalidation
	In order to implement caching standard
	As an HTTP client
	CacheCow must invalidate resource correctly

@cachecow_server
Scenario Outline: Invalidate collection
	Given I have an API running CacheCow Server and using <EntityTagStore> storage 
	And I Create a new item
	And Get the collection ETag as ETag1
	When I create a new item
	And Get the collection ETag as ETag2
	Then I expect ETag1 to be different from ETag2

Examples: 
| EntityTagStore |
| InMemory       |



@cachecow_server
Scenario Outline: Invalidate instance
	Given I have an API running CacheCow Server and using <EntityTagStore> storage 
	And I Create a new item
	And Get the instance ETag as ETag1
	When I update the item
	And Get the instance ETag as ETag2
	Then I expect ETag1 to be different from ETag2

Examples: 
| EntityTagStore |
| InMemory       |

@cachecow_server
Scenario Outline: Invalidate collection when instance changes
	Given I have an API running CacheCow Server and using <EntityTagStore> storage 
	And I Create a new item
	And Get the collection ETag as ETag1
	When I update the item
	And Get the collection ETag as ETag2
	Then I expect ETag1 to be different from ETag2

Examples: 
| EntityTagStore |
| InMemory       |