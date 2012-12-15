SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeleteCacheById]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[DeleteCacheById]
GO

-- =============================================
-- Author:		Ali Kheyrollahi
-- Create date: 2012-12-12
-- Description:	Deletes a CacheKey record by its id
-- =============================================
CREATE PROCEDURE DeleteCacheById
	@CacheKeyHash BINARY(20) 
AS
BEGIN
	SET NOCOUNT OFF
	DELETE FROM dbo.Cache WHERE CacheKeyHash = @CacheKeyHash
	SELECT @@ROWCOUNT AS Deleted
END
GO
