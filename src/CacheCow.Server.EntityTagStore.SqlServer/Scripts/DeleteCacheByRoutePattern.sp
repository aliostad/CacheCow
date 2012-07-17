SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeleteCacheByRoutePattern]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].DeleteCacheByRoutePattern
GO

-- =============================================
-- Author:		Ali Kheyrollahi
-- Create date: 2012-07-12
-- Description:	Deletes all CacheKey records by its route pattern
-- =============================================
CREATE PROCEDURE DeleteCacheByRoutePattern
	@routePattern NVARCHAR(256) 
AS
BEGIN
	SET NOCOUNT OFF

	DELETE FROM dbo.CacheState WHERE RoutePattern = @routePattern
END
GO
