namespace DbMigration.Common.Legacy.Model.Jobs
{
    /// <summary>
    /// This is a generic Task. Jobs can start / orchestrate multiple tasks. A task can be a table scan, connection test, index scan aso. 
    /// </summary>
    public class DbTask<T>
    {
        //A task wraps work that can be completed within 10 minutes. Tasks can be sent as queue messages, consumed and executed by a worker

        //Valid for - DbConnection types, transformation job, ...

        //State. (Running, stopped)
        //Desired state (Cancel, Pause, Start)

        //Consider how to handle messaging. Should each message be a task, or should it be possible to pause or stop a task? Or both?

        //Task stated is stored in table storage. 

        //Input should be a typed object, containing data. Not reference to a row to get. Then data can be loaded in bulk before, and tasks issued as messages. 

        public T Input { get; set; }

        public virtual void ValidateInput()
        {
            if (Input == null)
            {
                throw new ArgumentNullException(nameof(Input));
            }

        }

        public virtual void Run()
        {
            //Run the task. 
            //Update the state. 
            //Send messages to the queue. 

            throw new NotImplementedException();
        }

    }
}
