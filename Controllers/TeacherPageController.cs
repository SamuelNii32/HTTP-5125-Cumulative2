using HTTP_5125_Cumulative2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace HTTP_5125_Cumulative2.Controllers
{
    public class TeacherPageController : Controller
    {
        // Relying on API controller to handle teacher logic
        private readonly TeacherAPIController _api;

        public TeacherPageController(TeacherAPIController api)
        {
            _api = api;
        }

        // GET: TeacherPage/List?SearchKey={SearchKey}
        [HttpGet]
        public IActionResult List(string SearchKey)
        {
            List<Teacher> Teachers = _api.ListTeachers(SearchKey);
            return View(Teachers);
        }

        // GET: TeacherPage/Show/{id}
        [HttpGet]
        public IActionResult Show(int id)
        {
            Teacher SelectedTeacher = _api.FindTeacher(id);
            return View(SelectedTeacher);
        }

        // GET: TeacherPage/Add
        [HttpGet]
        public IActionResult Add(int id)
        {
            return View();
        }

        [HttpPost]
        
        public IActionResult Create(Teacher NewTeacher)
        {
            // Call the API method to add the teacher
            ActionResult<int> result = _api.AddTeacher(NewTeacher);

            // Check if the result is successful
            if (result.Result is OkObjectResult okResult)
            {
                int TeacherId = (int)okResult.Value;
                // Redirect to the Show page with the newly created TeacherId
                return RedirectToAction("Show", new { id = TeacherId });
            }

            // If there is an error, return a BadRequest response
            return BadRequest();
        }





        // GET: TeacherPage/DeleteConfirm/{id}
        [HttpGet]
        public IActionResult DeleteConfirm(int id)
        {
            Teacher SelectedTeacher = _api.FindTeacher(id);
            return View(SelectedTeacher);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            ActionResult result = _api.DeleteTeacher(id);
            if (result is NoContentResult)
            {
                Console.WriteLine("Redirecting to List");
                return RedirectToAction("List");
            }
            else if (result is NotFoundObjectResult)
            {
                // Optionally handle the "teacher not found" case
                return NotFound("Teacher not found, cannot delete.");
            }
            return BadRequest("An error occurred while deleting the teacher.");
        }
    }
}
