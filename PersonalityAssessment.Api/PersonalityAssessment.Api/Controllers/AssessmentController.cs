using Microsoft.AspNetCore.Mvc;
using PersonalityAssessment.Api.Services;
using PersonalityAssessment.Api.Models;

namespace PersonalityAssessment.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssessmentController : ControllerBase
    {
        private readonly IAssessmentService _assessmentService;
        private readonly IQuestionService _questionService;

        public AssessmentController(IAssessmentService assessmentService, IQuestionService questionService)
        {
            _assessmentService = assessmentService;
            _questionService = questionService;
        }

        [HttpGet("questions")]
        public async Task<ActionResult<List<QuestionWithChoicesDto>>> GetQuestions()
        {
            try
            {
                var questions = await _questionService.GetAllQuestionsAsync();
                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading questions", error = ex.Message });
            }
        }

        [HttpPost("seed-questions")]
        public async Task<ActionResult> SeedQuestions()
        {
            try
            {
                await _questionService.SeedSampleQuestionsAsync();
                return Ok(new { message = "Sample questions seeded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error seeding questions", error = ex.Message });
            }
        }

        [HttpPost("submit")]
        public async Task<ActionResult<AssessmentResult>> SubmitAssessment([FromBody] AssessmentRequest request, [FromQuery] int? userId = null)
        {
            try
            {
                var result = await _assessmentService.CalculateResultsAsync(request, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error processing assessment", 
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("demo")]
        public async Task<ActionResult<AssessmentResult>> GetDemoResult()
        {
            try
            {
                // Create a demo assessment request
                var demoRequest = new AssessmentRequest
                {
                    Answers = new List<Answer>
                    {
                        new Answer { QuestionId = 1, Value = 4 },
                        new Answer { QuestionId = 2, Value = 2 },
                        new Answer { QuestionId = 3, Value = 5 },
                        new Answer { QuestionId = 4, Value = 1 },
                        new Answer { QuestionId = 5, Value = 4 },
                        new Answer { QuestionId = 6, Value = 2 },
                        new Answer { QuestionId = 7, Value = 2 },
                        new Answer { QuestionId = 8, Value = 4 },
                        new Answer { QuestionId = 9, Value = 5 },
                        new Answer { QuestionId = 10, Value = 1 }
                    }
                };
                var result = await _assessmentService.CalculateResultsAsync(demoRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating demo result", error = ex.Message });
            }
        }
    }
}
