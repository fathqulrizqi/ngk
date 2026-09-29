-- Adds a "0" score option to Competency Result scoring (previously 5-1 only).
-- Changes:
--   1. Score is no longer coalesced to 0 for unscored lines, so an unscored line
--      (Score = NULL) is distinguishable from a line explicitly scored 0.
--   2. Adds a result0 column (checkbox state for a score of 0) alongside the
--      existing result1..result5 columns.
ALTER PROCEDURE [dbo].[sp_CompetencyResult]
	-- Add the parameters for the stored procedure here
	@periodFY varchar(5)
	,@NIK varchar(9)
	,@divName varchar(250)
	,@deptName varchar(250)
	,@sectName varchar(250)
	,@costName varchar(250)
	--,@positionName varchar(250)
	,@titleName varchar(250)
	,@tableType char(1) = 'A'
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	declare @_periodFY as varchar(5) = @periodFY;
	declare @_NIK as varchar(9) = @NIK;
	declare @_divName as varchar(250) = @divName;
	declare @_deptName as varchar(250) = @deptName;
	declare @_sectName as varchar(250) = @sectName;
	declare @_costName as varchar(250) = @costName;
	--declare @_positionName as varchar(250) = @positionName;
	declare @_titleName as varchar(250) = @titleName;
	declare @_tableType as char(1) = @tableType;

    -- Insert statements for procedure here
	/****** Script for SelectTopNRows command from SSMS  ******/
with tb0 as(
SELECT [id]
	  FROM [NGKBusi_Dev].[dbo].[HC_Competency_Map_Header]
	  where [division] = @_divName
		  and [department] = @_deptName
		  and [section] = @_sectName
		  and [costname] = @_costName
		  --and [job_position] = @_positionName
		  and [titlename] = @_titleName
		  ),tb1 as (
SELECT  [id]
      ,[header_id]
      ,[no]
      ,[requirement]
      ,[score]
      ,[module]
      ,[internal]
      ,[internal_duration]
      ,[external]
      ,[external_duration]
      ,[trainer]
      ,[evaluation_method]
      ,[remark]
      ,[idx]
      ,[hashcode]
  FROM [NGKBusi_Dev].[dbo].[HC_Competency_Map_Line]
  where header_id in (
	  select id from tb0
  ) and no is not null
)
,idxA as(
 select idx from tb1 where no = 'A. Pengetahuan Teknis / Technical Knowledge'
),idxB as(
 select idx from tb1 where no = 'B. Kemampuan Praktik / Practical Skill'
),idxC as(
 select idx from tb1 where no = 'C. Perilaku / Behaviour'
),tblA as(
select tHeader.guid, tHeader.nik
,isnull(tResult.header_id, ta.header_id) as Header_id
,isnull(tResult.no, ta.no) as No
,isnull(tResult.requirement, ta.requirement) as Requirement
,isnull(tResult.standard_score, ta.score) as Standard_Score
,tResult.score as Score
,tResult.result as Result
,tResult.note as Note
,isnull(tResult.idx, ta.idx) as IDX
,isnull(tResult.hashcode, ta.hashcode) as HashCode
from (
	select * from tb1 where idx >= (select idx from idxA) and idx < (select idx from idxB)
	) ta
	left outer join HC_Competency_Result_Line tResult
	on ta.no = tResult.no and isnull(ta.requirement,0) = isnull(tResult.requirement,0) and tResult.header_id = (select isnull(id,0) as id from HC_Competency_Result_Header where period_FY = @_periodFY and nik = @_NIK)
	left outer join (select * from HC_Competency_Result_Header where period_FY = @_periodFY and nik = @_NIK) tHeader
	on tHeader.id = tResult.header_id
),tblB as(
select tHeader.guid, tHeader.nik
,isnull(tResult.header_id, ta.header_id) as Header_id
,isnull(tResult.no, ta.no) as No
,isnull(tResult.requirement, ta.requirement) as Requirement
,isnull(tResult.standard_score, ta.score) as Standard_Score
,tResult.score as Score
,tResult.result as Result
,tResult.note as Note
,isnull(tResult.idx, ta.idx) as IDX
,isnull(tResult.hashcode, ta.hashcode) as HashCode
from (
 select * from tb1 where idx >= (select idx from idxB) and idx < (select idx from idxC)
	) ta
	left outer join HC_Competency_Result_Line tResult
	on ta.no = tResult.no and isnull(ta.requirement,0) = isnull(tResult.requirement,0) and tResult.header_id = (select isnull(id,0) as id from HC_Competency_Result_Header where period_FY = @_periodFY and nik = @_NIK)
	left outer join HC_Competency_Result_Header tHeader
	on tHeader.id = tResult.header_id and tHeader.period_FY = @_periodFY and tHeader.nik = @_NIK
),tblC as(
select tHeader.guid, tHeader.nik
,isnull(tResult.header_id, ta.header_id) as Header_id
,isnull(tResult.no, ta.no) as No
,isnull(tResult.requirement, ta.requirement) as Requirement
,isnull(tResult.standard_score, ta.score) as Standard_Score
,tResult.score as Score
,tResult.result as Result
,tResult.note as Note
,isnull(tResult.idx, ta.idx) as IDX
,isnull(tResult.hashcode, ta.hashcode) as HashCode
from (
 select * from tb1 where idx >= (select idx from idxC)
	) ta
	left outer join HC_Competency_Result_Line tResult
	on ta.no = tResult.no and isnull(ta.requirement,0) = isnull(tResult.requirement,0) and tResult.header_id = (select isnull(id,0) as id from HC_Competency_Result_Header where period_FY = @_periodFY and nik = @_NIK)
	left outer join HC_Competency_Result_Header tHeader
	on tHeader.id = tResult.header_id and tHeader.period_FY = @_periodFY and tHeader.nik = @_NIK
), tblAll as(
 select 'A' as tableType,* from tblA
 union all
 select 'B' as tableType,* from tblB
 union all
 select 'C' as tableType,* from tblC
)

select No,Requirement,Standard_Score,
case when Score = 5 then 1 else 0 end as result5,
case when Score = 4 then 1 else 0 end as result4,
case when Score = 3 then 1 else 0 end as result3,
case when Score = 2 then 1 else 0 end as result2,
case when Score = 1 then 1 else 0 end as result1,
case when Score = 0 then 1 else 0 end as result0,
Score,Result,Note from tblAll
where (header_id = isnull((select id from HC_Competency_Result_Header where period_FY = @_periodFY and nik = @_NIK) ,(select id from tb0)) and tableType = @_tableType)
order by idx


END
