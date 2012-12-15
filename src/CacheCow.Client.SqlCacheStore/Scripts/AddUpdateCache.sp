SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AddUpdateCache]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].AddUpdateCache
GO

-- =============================================
-- Author:		Ali Kheyrollahi
-- Create date: 2012-12-12
-- Description:	Adds or updates cache entry
-- =============================================
CREATE PROCEDURE AddUpdateCache
	@cacheKeyHash	BINARY(20),
	@Domain			NVARCHAR(256),
	@Size			INT,
	@CacheBlob		VARBINARY(MAX)	
	 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON

	BEGIN TRAN
	IF EXISTS (SELECT 1 FROM dbo.Cache 
			WITH (UPDLOCK,SERIALIZABLE) WHERE CacheKeyHash = @cacheKeyHash)
		BEGIN
			UPDATE dbo.Cache SET 
					Domain = @Domain,
					CacheBlob = @CacheBlob,
					Size = @Size,
					LastAccessed = GETDATE()
				WHERE CacheKeyHash = @cacheKeyHash
		END
	ELSE
	
		BEGIN
			INSERT INTO dbo.Cache
				(
				cacheKeyHash,
				Domain,
				Size,
				CacheBlob,
				LastAccessed)			
			values 
				(@cacheKeyHash, @Domain, @Size, @CacheBlob, GETDATE())
		END
	COMMIT TRAN

END
GO
