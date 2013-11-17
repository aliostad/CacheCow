SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Server_DeleteCacheByResourceUri]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].Server_DeleteCacheByResourceUri
GO

-- =============================================
-- Author:		Ali Kheyrollahi
-- Create date: 2013-11-16
-- Description:	Deletes all CacheKey records by its resource uri
-- =============================================
CREATE PROCEDURE Server_DeleteCacheByResourceUri
	@resourceUri NVARCHAR(256) 
AS
BEGIN
	SET NOCOUNT OFF

	DELETE FROM dbo.CacheState WHERE ResourceUri = @resourceUri
END
GO
