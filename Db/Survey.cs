using Microsoft.EntityFrameworkCore;

namespace Telegram.Messaging.Db
{
	public class Survey
	{

#if RELEASE
		public const int SURVEY_EXPIRES_AFTER_SECONDS = 60 * 24 * 30 * 60;
#else
		public const int SURVEY_EXPIRES_AFTER_SECONDS = 30 * 60;
#endif

		public int Id { get; set; }
		/// <summary>
		/// This the Message Id assigned by telegram to our sent message.
		/// It is null as long as the message hasn't been sent and hence we don't have an ID 
		/// </summary>
		public int? TelegramMessageId { get; set; }
		public long TelegramUserId { get; set; }
		public bool IsActive { get; set; }
		public bool IsCancelled { get; set; }
		public bool IsCompleted { get; set; }
		public DateTime CreatedUtc { get; set; }
		public DateTime LastInteractionUtc { get; set; }
		public List<Question> Questions { get; set; }

		public Survey()
		{
			Questions = new List<Question>();
			CreatedUtc = DateTime.UtcNow;
			LastInteractionUtc = DateTime.UtcNow;
		}

		/// <summary>
		/// Retrieves the most recent Active  and not Completed survey or null if there is not 
		/// </summary>
		/// <returns></returns>
		public static async Task<Survey?> GetCurrentSurvey(long telegramUserId)
		{
			using (var db = new MessagingDb())
			{
				var ss = await (from survs in db.Surveys.Include(x => x.Questions)//.ThenInclude(y => y.FieldType)
								where survs.IsActive && survs.IsCompleted == false && survs.IsCancelled == false
								&& survs.LastInteractionUtc.AddSeconds(SURVEY_EXPIRES_AFTER_SECONDS) > DateTime.UtcNow
								&& telegramUserId == survs.TelegramUserId
								select survs).OrderByDescending(x => x.TelegramMessageId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
				if (ss != null)
					ss.Questions = ss.Questions.OrderBy(x => x.Id).ToList();

				return ss;
			}
		}

		/// <summary>
		/// Retrieves the most recent Completed survey or null if there is not 
		/// </summary>
		/// <returns></returns>
		public static async Task<Survey?> GetRecentCompletedSurvey(long telegramUserId)
		{
			using (var db = new MessagingDb())
			{
				var ss = await (from survs in db.Surveys.Include(x => x.Questions)//.ThenInclude(y => y.FieldType)
								where survs.IsCompleted == true && survs.TelegramUserId == telegramUserId
								select survs).OrderByDescending(x => x.TelegramMessageId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

				if (ss != null)
					ss.Questions = ss.Questions.OrderBy(x => x.Id).ToList();

				return ss;
			}
		}

		/// <summary>
		/// Retrieves the Survey through the answer id
		/// </summary>
		/// <returns></returns>
		public static async Task<Survey?> GetSurveyByQuestionId(int questionId)
		{
			using (var db = new MessagingDb())
			{
				var ss = await (from survs in db.Surveys.Include(x => x.Questions)//.ThenInclude(y => y.FieldType)
								join quests in db.Questions on survs.Id equals quests.SurveyId
								where quests.Id == questionId
								select survs).OrderByDescending(x => x.TelegramMessageId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

				if (ss != null)
					ss.Questions = ss.Questions.OrderBy(x => x.Id).ToList();

				return ss;
			}
		}

		/// <summary>
		/// Gets the most recent shown survey, can be active/completed, or null if none was found
		/// </summary>
		/// <param name="tid">The user to whom the survey was shown</param>
		/// <returns></returns>
		public static async Task<Survey?> GetMostRecent(long tid)
		{
			using (var db = new MessagingDb())
			{
				var ss = await (from survs in db.Surveys.Include(x => x.Questions)//.ThenInclude(y => y.FieldType)
								where tid == survs.TelegramUserId
								select survs).OrderByDescending(x => x.TelegramMessageId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
				if (ss != null)
					ss.Questions = ss.Questions.OrderBy(x => x.Id).ToList();

				return ss;
			}
		}

		/// <summary>
		/// Adds a survey to the DB
		/// </summary>
		/// <param name="telegramMessageId"></param>
		/// <param name="userId"></param>
		/// <returns></returns>
		public static async Task<Survey?> AddSurvey(int? telegramMessageId, long userId)
		{
			using (var db = new MessagingDb())
			{
				var survey = new Survey()
				{
					CreatedUtc = DateTime.UtcNow,
					IsActive = true,
					IsCancelled = false,
					IsCompleted = false,
					TelegramMessageId = telegramMessageId,
					TelegramUserId = userId
				};
				db.Surveys.Add(survey);
				await db.SaveChangesAsync();
				return survey;
			}
		}

		/// <summary>
		/// Updates this survey to the DB
		/// </summary> 
		/// <param name="updateQuestions">Whether the answers should be updated</param>
		/// <returns></returns>
		public async Task UpdateSurvey(bool updateQuestions)
		{
			using (var db = new MessagingDb())
			{
				db.Entry(this).State = EntityState.Modified;
				LastInteractionUtc = DateTime.UtcNow;
				if (updateQuestions)
					Questions.ForEach(q =>
					{
						db.Attach(q);
						var entry = db.Entry(q);
						if (entry.State != EntityState.Added)
							entry.State = EntityState.Modified;
					});
				await db.SaveChangesAsync();
			}
		}

		public Question? MostRecentQuestion
		{
			get
			{
				if (Questions.Count == 0) return null;
				return Questions[Questions.Count - 1];
			}
		}

		public override string ToString()
		{
			return $"{Id}|{TelegramMessageId}|{TelegramUserId}";
		}
	}
}
