SELECT TOP (1000) [Id]
      ,[TelegramMessageId]
      ,[TelegramUserId]
      ,[IsActive]
      ,[IsCancelled]
      ,[IsCompleted]
      ,[CreateUtc]
  FROM [Surveys]
  order by id desc

  select * 
  from Questions
  order by id desc
  
  select *  
  from logs where message like '%' order by id desc
  

 select * from requests order by LastUpdateUtc desc



