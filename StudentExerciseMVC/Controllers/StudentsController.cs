using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using StudentExerciseMVC.Models;
using StudentExerciseMVC.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace StudentExerciseMVC.Controllers
{
    public class StudentsController : Controller
    {
        private readonly IConfiguration _config;

        public StudentsController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }


        // GET: All Students
        public ActionResult Index()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT s.Id, s.FirstName, s.LastName, s.SlackHandle, 
                                        s.CohortId, c.Id, c.CohortName
                                        FROM Student s
                                        LEFT JOIN Cohort c ON s.CohortId = c.Id";

                    var reader = cmd.ExecuteReader();

                    var students = new List<Student>();

                    while (reader.Read())
                    {
                        students.Add(new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                            CohortId = reader.GetInt32(reader.GetOrdinal("CohortId")),
                            Cohort = new Cohort
                            {
                                CohortName = reader.GetString(reader.GetOrdinal("CohortName"))
                            }
                        });
                    }
                    reader.Close();
                    return View(students);

                }
            }

        }

        // GET: Students/Details/5
        public ActionResult Details(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT s.Id, s.FirstName, s.LastName, s.SlackHandle, 
                                        s.CohortId, c.Id, c.CohortName
                                        FROM Student s
                                        LEFT JOIN Cohort c ON s.CohortId = c.Id     
                                        WHERE s.Id = @Id";
                    cmd.Parameters.Add(new SqlParameter("@Id", id));
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var student = new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                            CohortId = reader.GetInt32(reader.GetOrdinal("CohortId")),
                            Cohort = new Cohort
                            {
                                CohortName = reader.GetString(reader.GetOrdinal("CohortName"))
                            }

                        };
                        // GetExercisesByStudentId private method to get all exercises for student details
                        student.Exercises = GetExercisesByStudentId(id);

                        reader.Close();
                        return View(student);
                    }
                    reader.Close();
                    return NotFound();
                }
            }
        }


        // GET: Students/Create
        public ActionResult Create()
        {

            var cohorts = GetCohorts().Select(c => new SelectListItem
            {
                Text = c.CohortName,
                Value = c.Id.ToString()
            }).ToList();

            var exercises = GetExercises().Select(e => new SelectListItem
            {
                Text = e.ExerciseName,
                Value = e.Id.ToString()
            }).ToList();

            var viewModel = new StudentViewModel()
            {
                Student = new Student(),
                Cohorts = cohorts,
                Exercises = exercises
            };

            return View(viewModel);
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Student student)
        {
            try
            {


                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"INSERT INTO Student (FirstName, LastName, SlackHandle, CohortId)
                                           OUTPUT INSERTED.Id
                                            VALUES (@firstName, @lastName, @slackHandle, @cohortId)";

                        cmd.Parameters.Add(new SqlParameter("@firstName", student.FirstName));
                        cmd.Parameters.Add(new SqlParameter("@lastName", student.LastName));
                        cmd.Parameters.Add(new SqlParameter("@slackHandle", student.SlackHandle));
                        cmd.Parameters.Add(new SqlParameter("@cohortId", student.CohortId));

                        // stores the ouputed id
                        int newId = (int)cmd.ExecuteScalar();
                        student.Id = newId;

                    }
                }
                // Private Method to add exercises to a student
                AddStudentExercises(student.Id, student.ExerciseIds);

                return RedirectToAction(nameof(Index));

            }
            catch
            {
                return View();
            }
        }

        // GET: Students/Edit/5
        public ActionResult Edit(int id)
        {
            var cohorts = GetCohorts().Select(c => new SelectListItem
            {
                Text = c.CohortName,
                Value = c.Id.ToString()
            }).ToList();

            var exercises = GetExercises().Select(e => new SelectListItem
            {
                Text = e.ExerciseName,
                Value = e.Id.ToString()
            }).ToList();

            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, FirstName, LastName, SlackHandle, CohortId
                                        FROM Student
                                        WHERE Id = @id";

                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var student = new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                            CohortId = reader.GetInt32(reader.GetOrdinal("CohortId"))
                        };

                        reader.Close();

                        // Shows new viewModel with Student object, Cohort, and exercises
                        var viewModel = new StudentViewModel
                        {
                            Student = student,
                            Cohorts = cohorts,
                            Exercises = exercises
                        };
                        return View(viewModel);
                    }

                    reader.Close();
                    return NotFound();
                }
            }

        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Student student)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Student
                                            Set FirstName = @firstName,
                                                LastName = @lastName,
                                                SlackHandle = @slackHandle,
                                                CohortId = @cohortId                                           
                                                WHERE Id = @id";

                        cmd.Parameters.Add(new SqlParameter("@firstName", student.FirstName));
                        cmd.Parameters.Add(new SqlParameter("@lastName", student.LastName));
                        cmd.Parameters.Add(new SqlParameter("@slackHandle", student.SlackHandle));
                        cmd.Parameters.Add(new SqlParameter("@cohortId", student.CohortId));
                        cmd.Parameters.Add(new SqlParameter("Id", id));

                        cmd.ExecuteNonQuery();
                    }
                }
                
                //Private Methods to edit a student's exercises
                DeleteAssignedExercises(student.Id);
                AddStudentExercises(student.Id, student.ExerciseIds);
                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                return View();
            }
        }


        // GET: Students/Delete/5
        public ActionResult Delete(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, FirstName, LastName, SlackHandle, CohortId
                                        FROM Student
                                        WHERE Id = @Id";

                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var student = new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                            CohortId = reader.GetInt32(reader.GetOrdinal("CohortId"))
                        };

                        reader.Close();
                        return View(student);
                    }
                    return NotFound();
                }
            }
        }

        // POST: Students/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Student student)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Student WHERE Id = @id";

                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        cmd.ExecuteNonQuery();

                        return RedirectToAction(nameof(Index));
                    }
                }

            }
            catch
            {
                return View();
            }
        }

        // GET: Private method to get a list of Cohorts
        private List<Cohort> GetCohorts()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, CohortName
                                       FROM Cohort";

                    var reader = cmd.ExecuteReader();

                    var cohorts = new List<Cohort>();

                    while (reader.Read())
                    {
                        cohorts.Add(new Cohort
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            CohortName = reader.GetString(reader.GetOrdinal("CohortName"))
                        });
                    }

                    reader.Close();

                    return cohorts;
                }
            }
        }

        // GET: Private method to get a list of Exercises
        private List<Exercise> GetExercises()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, ExerciseName, ExerciseLanguage
                                       FROM Exercise";

                    var reader = cmd.ExecuteReader();

                    var exercises = new List<Exercise>();

                    while (reader.Read())
                    {
                        exercises.Add(new Exercise
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                            ExerciseLanguage = reader.GetString(reader.GetOrdinal("ExerciseLanguage"))
                        });
                    }

                    reader.Close();

                    return exercises;
                }
            }

        }


        //CREATE: Private method to add xercises to a student
        private void AddStudentExercises(int StudentId, List<int> ExerciseIds)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                foreach (var exerciseId in ExerciseIds)
                {

                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"INSERT INTO StudentExercise (StudentId, ExerciseId)
                                            VALUES (@studentId, @exerciseId)";

                        cmd.Parameters.Add(new SqlParameter("@studentId", StudentId));
                        cmd.Parameters.Add(new SqlParameter("@exerciseId", exerciseId));

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // DELETE: Private delete function used in the edit 
        private void DeleteAssignedExercises(int StudentId)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"DELETE FROM StudentExercise WHERE StudentId = @studentId";

                    cmd.Parameters.Add(new SqlParameter("studentid", StudentId));

                    cmd.ExecuteNonQuery();
                }
            }
        }

        //GET: Private method to get all exercises by StudentId
        private List<Exercise> GetExercisesByStudentId(int StudentId)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT e.Id AS ExerciseId, e.ExerciseName, e.ExerciseLanguage
                                       FROM Exercise e
                                       INNER JOIN StudentExercise se ON e.Id = se.ExerciseId
                                        WHERE StudentId = @StudentId";

                    cmd.Parameters.AddWithValue("StudentId", StudentId);

                    var reader = cmd.ExecuteReader();

                    List<Exercise> exercises = new List<Exercise>();

                    while (reader.Read())
                    {
                        Exercise exercise = new Exercise
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("ExerciseId")),
                            ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                            ExerciseLanguage = reader.GetString(reader.GetOrdinal("ExerciseLanguage")),
                        };
                        exercises.Add(exercise);

                    };
                    reader.Close();
                    return exercises;

                }
            }
        }

    }
}
