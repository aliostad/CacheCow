SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetDomainSizes]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].GetDomainSizes
GO

-- =============================================
-- Author:		Ali Kheyrollahi
-- Create date: 2012-12-12
-- Description:	returns each domain with its total size
-- =============================================
CREATE PROCEDURE GetDomainSizes
	 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT 
		Domain,
		SUM(Size) As TotalSize
	FROM
		dbo.Cache
	GROUP BY
		Domain

END
GO
