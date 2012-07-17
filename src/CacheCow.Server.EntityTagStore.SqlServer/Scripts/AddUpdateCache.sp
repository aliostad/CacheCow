SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AddUpdateCache]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].AddUpdateCache
GO

-- =============================================
-- Author:		Ali Kheyrollahi
-- Create date: 2012-07-12
-- Description:	Adds or updates cache entry
-- =============================================
CREATE PROCEDURE AddUpdateCache
	@cacheKeyHash	BINARY(20),
	@routePattern	NVARCHAR(256),
	@eTag			NVARCHAR(100),
	@lastModified	DATETIME
	 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON

	BEGIN TRAN
	IF EXISTS (SELECT 1 FROM dbo.CacheState 
			WITH (UPDLOCK,SERIALIZABLE) WHERE CacheKeyHash = @cacheKeyHash)
		BEGIN
			UPDATE dbo.CacheState SET 
					ETag = @eTag,
					LastModified = @lastModified
				WHERE CacheKeyHash = @cacheKeyHash
		END
	ELSE
	
		BEGIN
			INSERT INTO dbo.CacheState 
				(CacheKeyHash, RoutePattern, ETag, LastModified)
			values 
				(@cacheKeyHash, @routePattern, @eTag, @lastModified)
		END
	COMMIT TRAN

END
GO
