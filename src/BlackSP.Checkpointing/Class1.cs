using System;

namespace BlackSP.Checkpointing
{
    public class Class1
    {

        //what is state?
        //- user operator class instance (in its entirity)
        //- operatorshells windows
        //- some counters in middlewares
        //- checkpointing protocol related object-properties

        //do we want to serialize the entire class instance of the operator?
        //- require serializable attribute and make user responsible

        //perhaps annotate properties with attributes to mark them as part of the state
        //- what if complex object?
        //-- have user override serialize method (attribute + ISerializable) for refined serialization control

        //during startup
        //- any object on constructed --> register with cp manager (only store those that have serializable types) (log info that it was dropped)
        //- find unique keys for each object's marked properties (consistently the same.. can assume same startup order)
        //On autofac component instance activated --> register with checkpointmanager
        //-- https://stackoverflow.com/questions/6048812/autofac-add-onactivated-to-all-registrations

        //on CP 
        //- iterate registered objects
        //- iterate properties with XXXAttribute
        //-- serialize property value with key (based on name?)
        //- serialize map and give ID
        //-- need middleware to store received cp clocks from upstreams

        //on restore --> iterate registered objects
        //- pull in blob by GUID
        //- deserialize into map with key / blob
        //- iterate keys
        //- lookup object associated with key
        //- overwrite property value in object with deserialized blob

    }
}
