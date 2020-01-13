using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace StudentExerciseMVC.Models
{
    public class Cohort
    {
        public int Id { get; set; }

        [Display(Name = "Cohort")]
        public string CohortName { get; set; }
    }
}
