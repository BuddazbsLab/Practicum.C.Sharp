using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using School.Data;
using System.Globalization;
using System.Data;
using System.Data.Objects;

namespace School
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Connection to the School database
        private SchoolDBEntities schoolContext = null;

        // Field for tracking the currently selected teacher
        private Teacher teacher = null;

        // List for tracking the students assigned to the teacher's class
        private IList studentsInfo = null;

        #region Predefined code

        public MainWindow()
        {
            InitializeComponent();
        }

        // Connect to the database and display the list of teachers when the window appears
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.schoolContext = new SchoolDBEntities();
            teachersList.DataContext = this.schoolContext.Teachers;
        }

        // When the user selects a different teacher, fetch and display the students for that teacher
        private void TeachersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Find the teacher that has been selected
            this.teacher = teachersList.SelectedItem as Teacher;
            this.schoolContext.LoadProperty(this.teacher, s => s.Students);

            // Find the students for this teacher
            this.studentsInfo = ((IListSource)teacher.Students).GetList();

            // Use databinding to display these students
            studentsList.DataContext = this.studentsInfo;
        }

        #endregion

        // When the user presses a key, determine whether to add a new student to a class, remove a student from a class, or modify the details of a student
        private void StudentsList_KeyDown(object sender, KeyEventArgs e)
        {
            // TODO: Exercise 1: Task 1a: If the user pressed Enter, edit the details for the currently selected student
            switch (e.Key)
            {
                case Key.Enter:
                    Student student = this.studentsList.SelectedItem as Student;
                    EditStudent(student);
                    break;

                    // TODO: Exercise 2: Task 1a: If the user pressed Insert, add a new student
                    case Key.Insert:
                    AddnewStudent();
                    break;

                // TODO: Exercise 3: Task 1a: If the user pressed Delete, remove the currently selected student

                case Key.Delete:
                    Student deleteStudent = this.studentsList.SelectedItem as Student;
                    RemoveStudent(deleteStudent);
                    break;                                      
            }
        }
        #region Predefined code

        private void StudentsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditStudent(this.studentsList.SelectedItem as Student);
        }

        // Remove the details of a student
        private void RemoveStudent(Student student)
        {
            // TODO: Exercise 3: Task 2a: Prompt the user to confirm that the student should be removed
            MessageBoxResult response = MessageBox.Show(string.Format($"Delete{student.FirstName + " " + student.LastName }"), "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            // TODO: Exercise 3: Task 3a: If the user clicked Yes, remove the student from the database
            if (response == MessageBoxResult.Yes)
            {
                this.schoolContext.Students.DeleteObject(student);
                // TODO: Exercise 3: Task 3b: Enable saving (changes are not made permanent until they are written back to the database)
                saveChanges.IsEnabled = true;
            }
        }

        // Add a new student. 
        private void AddnewStudent()
        {
            // Use the StudentsForm to get the details of the student from the user
            StudentForm studentForm = new StudentForm
            {
                // Set the title of the form to indicate which class the student will be added to (the class for the currently selected teacher)
                Title = "New Student" + teacher.Class
            };

            // Display the form and get the details of the new student
            if (studentForm.ShowDialog().Value)
            {
                // When the user closes the form, retrieve the details of the student from the form
                // and use them to create a new Student object
                Student newStudent = new Student
                {
                    FirstName = studentForm.firstName.Text,
                    LastName = studentForm.lastName.Text,
                    DateOfBirth = DateTime.Parse(studentForm.dateOfBirth.Text)
                };

                // Assign the new student to the current teacher
                this.teacher.Students.Add(newStudent);

                // Add the student to the list displayed on the form
                this.studentsInfo.Add(newStudent);

                // Enable saving (changes are not made permanent until they are written back to the database)
                saveChanges.IsEnabled = true;
            }
        }


        // Edit the details of a student
        private void EditStudent(Student student)
        {
            // TODO: Exercise 1: Task 2a: Use the StudentsForm to display and edit the details of the student
            StudentForm studentForm = new StudentForm
            {
                // TODO: Exercise 1: Task 2b: Set the title of the form and populate the fields on the form with the details of the student
                Title = "Edit Student BIO"
            };
            studentForm.firstName.Text = student.FirstName;
            studentForm.lastName.Text = student.LastName;
            studentForm.dateOfBirth.Text = student.DateOfBirth.ToString("MM/dd/yyyy");

            if (studentForm.ShowDialog().Value)
            {

                // TODO: Exercise 1: Task 3b: When the user closes the form, copy the details back to the student
                student.FirstName = studentForm.firstName.Text;
                student.LastName = studentForm.lastName.Text;
                student.DateOfBirth = DateTime.Parse(studentForm.dateOfBirth.Text);
                // TODO: Exercise 1: Task 3c: Enable saving (changes are not made permanent until they are written back to the database)
                saveChanges.IsEnabled = true;
            }
        }

        // Save changes back to the database and make them permanent
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save the changes
                this.schoolContext.SaveChanges();

                // Disable the Save button (it will be enabled if the user makes more changes)
                saveChanges.IsEnabled = false;
            }
            // Catch the common exceptions that can occur when saving changes
            catch (OptimisticConcurrencyException)
            {
                // If another user has changed the same students earlier, then overwrite their changes with the new data
                this.schoolContext.Refresh(RefreshMode.ClientWins, schoolContext.Students);
                this.schoolContext.SaveChanges();
            }
            catch (UpdateException uEx)
            {
                // If some sort of database exception has occured, then display the reason reported for the exception and rollback
                MessageBox.Show(uEx.InnerException.Message, "Error saving changes");
                this.schoolContext.Refresh(RefreshMode.StoreWins, schoolContext.Students);
            }
            catch (Exception ex)
            {
                // If some other exception occurs, report it to the user
                MessageBox.Show(ex.Message, "Error saving changes");
                this.schoolContext.Refresh(RefreshMode.ClientWins, schoolContext.Students);
            }

        }

        #endregion
    }

    [ValueConversion(typeof(string), typeof(decimal))]
    class AgeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                              CultureInfo culture)
        {
            // Convert the date of birth provided in the value parameter and convert to the age of the student in years
            // TODO: Exercise 4: Task 2a: Check that the value provided is not null. If it is, return an empty string
            if (value != null)
            {
                // TODO: Exercise 4: Task 2b: Convert the value provided into a DateTime value
                DateTime dateTimeBirthDauStudent = (DateTime)value;
                // TODO: Exercise 4: Task 2c: Work out the difference between the current date and the value provided
                TimeSpan workDifference = DateTime.Now.Subtract(dateTimeBirthDauStudent);
                // TODO: Exercise 4: Task 2d: Convert this result into a number of years
                int workDifferenceDays = workDifference.Days / 365;
                // TODO: Exercise 4: Task 2e: Convert the number of years into a string and return it
                return workDifferenceDays.ToString();
            }
            else
            {
                return "Something went wrong. Check your code!";
            }

            
        }

        #region Predefined code

        public object ConvertBack(object value, Type targetType, object parameter,
                                  CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
