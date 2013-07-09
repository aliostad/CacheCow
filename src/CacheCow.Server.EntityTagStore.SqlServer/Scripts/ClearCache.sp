SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ClearCache]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[ClearCache]
GO

-- =============================================
-- Author:		Carl Duguay
-- Create date:	2013-07-10
-- Description:	Removes all CacheKey records
-- =============================================
CREATE PROCEDURE ClearCache	 
AS
BEGIN
	SET NOCOUNT OFF
	DELETE FROM [dbo].[CacheState]
END
GO
