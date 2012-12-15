SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetEarliestAccessedItem]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].GetEarliestAccessedItem
GO

-- =============================================
-- Author:		Ali Kheyrollahi
-- Create date: 2012-12-12
-- Description:	returns earliest accessed item optionally for a domain
-- =============================================
CREATE PROCEDURE GetEarliestAccessedItem
	 @Domain	NVARCHAR(256)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF @Domain = ''
		SELECT TOP 1 
			CacheKeyHash,
			LastAccessed,
			Size,
			Domain
		FROM
			dbo.Cache
		ORDER BY
			LastAccessed
	ELSE
		SELECT TOP 1 
			CacheKeyHash,
			LastAccessed,
			Size,
			Domain
		FROM
			dbo.Cache
		WHERE
			Domain = @Domain
		ORDER BY
			LastAccessed
			
		


END
GO
