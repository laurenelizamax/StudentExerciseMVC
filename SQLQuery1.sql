SELECT e.Id AS ExerciseId, e.ExerciseName, e.ExerciseLanguage
                                       FROM Exercise e
                                       INNER JOIN StudentExercise se ON e.Id = se.ExerciseId
                                        WHERE StudentId = 2