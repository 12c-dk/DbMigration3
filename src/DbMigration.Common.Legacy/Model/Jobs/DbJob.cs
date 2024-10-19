namespace DbMigration.Common.Legacy.Model.Jobs
{

    public class DbJob<T>
    {
        //A job may be user invoked jobs. Jobs orchestrate tasks. 
        //A job may start many tasks, e.g. for checking index, Getting batch of 100 lines, writing batch of 100 lines. 
        //Function app orchestrates a job - sends messages for each task. 

        //public DbJob()
        //{

        //}

        public DbJob(T input)
        {
            Input = input;
        }

        public DbJob()
        {

        }

        public T Input { get; set; }

        protected virtual void ValidateInput()
        {
            if (Input == null)
            {
                throw new ArgumentNullException(nameof(Input));
            }

        }

        public virtual void Run()
        {
            ValidateInput();

            //Run the Job. 
            //Update the state. 
            //Send messages to the queue. 
        }

    }
}
