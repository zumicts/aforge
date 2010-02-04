// AForge Fuzzy Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2008-2009
// andrew.kirillov@aforgenet.com
//
// Copyright © Fabio L. Caversan, 2008-2009
// fabio.caversan@gmail.com
//

namespace AForge.Fuzzy
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class represents a Fuzzy Rule, a linguistic expression representing some behavioral
    /// aspect of a Fuzzy Inference System. 
    /// </summary>
    /// 
    /// <remarks><para>
    /// A Fuzzy Rule is a fuzzy linguistic instruction that can be executed by a fuzzy system.
    /// The format of the Fuzzy Rule is:
    /// </para>
    /// 
    /// <para><b>IF <i>antecedent</i> THEN <i>consequent</i></b></para>
    /// 
    /// <para>The antecedent is composed by a set of fuzzy clauses (see <see cref="Clause"/>) connected
    /// by fuzzy operations, like <b>AND</b> or <b>OR</b>: </para>
    /// 
    /// <para><b>...<i>Clause1</i> AND (<i>Clause2</i> OR <i>Clause3</i>) ...</b></para>
    ///     
    /// <para>The consequent is a single of fuzzy clauses (<see cref="Clause"/>). To perform the
    /// linguistic computing, the <see cref="Rule"/> evaluates the clauses and then applies the fuzzy
    /// operators. Once this is done a value representing the confidence in the antecedent being
    /// true is obtained, and this is called firing strength of the <see cref="Rule"/>.</para>
    /// 
    /// <para>The firing strength is used to discover with how much confidence the consequent
    /// of a rule is true.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create the linguistic labels (fuzzy sets) that compose the temperature 
    /// TrapezoidalFunction function1 = new TrapezoidalFunction(
    ///     10, 15, TrapezoidalFunction.EdgeType.Right );
    /// FuzzySet fsCold = new FuzzySet( "Cold", function1 );
    /// TrapezoidalFunction function2 = new TrapezoidalFunction( 10, 15, 20, 25 );
    /// FuzzySet fsCool = new FuzzySet( "Cool", function2 );
    /// TrapezoidalFunction function3 = new TrapezoidalFunction( 20, 25, 30, 35 );
    /// FuzzySet fsWarm = new FuzzySet( "Warm", function3 );
    /// TrapezoidalFunction function4 = new TrapezoidalFunction(
    ///     30, 35, TrapezoidalFunction.EdgeType.Left );
    /// FuzzySet fsHot = new FuzzySet( "Hot", function4 );
    /// 
    /// // create a linguistic variable to represent steel temperature
    /// LinguisticVariable lvSteel = new LinguisticVariable( "Steel", 0, 80 );
    /// // adding labels to the variable
    /// lvSteel.AddLabel( fsCold );
    /// lvSteel.AddLabel( fsCool );
    /// lvSteel.AddLabel( fsWarm );
    /// lvSteel.AddLabel( fsHot );
    /// 
    /// // create a linguistic variable to represent stove temperature
    /// LinguisticVariable lvStove = new LinguisticVariable( "Stove", 0, 80 );
    /// // adding labels to the variable
    /// lvStove.AddLabel( fsCold );
    /// lvStove.AddLabel( fsCool );
    /// lvStove.AddLabel( fsWarm );
    /// lvStove.AddLabel( fsHot );
    /// 
    /// // create a linguistic variable database
    /// Database db = new Database( );
    /// db.AddVariable( lvSteel );
    /// db.AddVariable( lvStove );
    /// 
    /// // sample rules just to test the expression parsing
    /// Rule r1 = new Rule( db, "Test1", "IF Steel is Cold and Stove is Hot then Pressure is Low" );
    /// Rule r2 = new Rule( db, "Test2", "IF Steel is Cold and (Stove is Warm or Stove is Hot) then Pressure is Medium" );
    /// Rule r3 = new Rule( db, "Test3", "IF Steel is Cold and Stove is Warm or Stove is Hot then Pressure is High" );
    /// 
    /// // testing the firing strength
    /// lvSteel.NumericInput = 12;
    /// lvStove.NumericInput = 35;
    /// double result r1.EvaluateFiringStrength( );
    /// </code>    
    /// </remarks>
    /// 
    public class Rule
    {
        // name of the rule 
        private string name;
        // the original expression 
        private string rule;
        // the parsed RPN (reverse polish notation) expression
        private List<object> rpnTokenList;
        // the consequento (output) of the rule
        private Clause output;
        // the database with the linguistic variables
        private Database database;
        // the norm operator
        private INorm normOperator;
        // the conorm operator
        private ICoNorm conormOperator;

        /// <summary>
        /// The name of the fuzzy rule.
        /// </summary>
        /// 
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The fuzzy <see cref="Clause"/> that represents the consequent of the <see cref="Rule"/>.
        /// </summary>
        /// 
        public Clause Output
        {
            get { return output; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class.
        /// </summary>
        /// 
        /// <param name="fuzzyDatabase">A fuzzy <see cref="Database"/> containig the linguistic variables
        /// (see <see cref="LinguisticVariable"/>) that will be used in the Rule.</param>
        /// 
        /// <param name="name">Name of this <see cref="Rule"/>.</param>
        /// 
        /// <param name="rule">A string representing the <see cref="Rule"/>. It must be a "IF..THEN" statement.
        /// For a more detailed  description see <see cref="Rule"/> class.</param>
        /// 
        /// <param name="normOperator">A class that implements a <see cref="INorm"/> interface to
        /// evaluate the AND operations of the Rule. </param>
        /// 
        /// <param name="coNormOperator">A class that implements a <see cref="ICoNorm"/> interface
        /// to evaluate the OR operations of the Rule. </param>
        /// 
        public Rule( Database fuzzyDatabase, string name, string rule, INorm normOperator, ICoNorm coNormOperator )
        {
            // the list with the RPN expression
            rpnTokenList = new List<object>( );

            // setting attributes
            this.name           = name;
            this.rule           = rule;
            this.database       = fuzzyDatabase;
            this.normOperator   = normOperator;
            this.conormOperator = coNormOperator;

            // parsing the rule to obtain RPN of the expression
            ParseRule( );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class using as
        /// CoNorm the <see cref="MaximumCoNorm"/> and as Norm the <see cref="MinimumNorm"/>.
        /// </summary>
        /// 
        /// <param name="fuzzyDatabase">A fuzzy <see cref="Database"/> containig the linguistic variables
        /// (see <see cref="LinguisticVariable"/>) that will be used in the <see cref="Rule"/>.</param>
        /// 
        /// <param name="name">Name of this <see cref="Rule"/>.</param>
        /// 
        /// <param name="rule">A string representing the <see cref="Rule"/>. It must be a "IF..THEN"
        /// statement. For a more detailed description see <see cref="Rule"/> class.</param>
        /// 
        public Rule( Database fuzzyDatabase, string name, string rule ) :
            this( fuzzyDatabase, name, rule, new MinimumNorm( ), new MaximumCoNorm( ) )
        {
        }

        /// <summary>
        /// Converts the RPN fuzzy expression into a string representation.
        /// </summary>
        /// 
        /// <returns>String representation of the RPN fuzzy expression.</returns>
        /// 
        public string GetRPNExpression( )
        {
            string result = "";
            foreach ( object o in rpnTokenList )
            {
                // if its a fuzzy clause we can call clause's ToString()
                if ( o.GetType( ) == typeof( Clause ) )
                {
                    Clause c = o as Clause;
                    result += c.ToString( );
                }
                else
                    result += o.ToString( );
                result += ", ";
            }
            result += "#";
            result = result.Replace( ", #", "" );
            return result;
        }

        /// <summary>
        /// Defines the priority of the fuzzy operators.
        /// </summary>
        /// 
        /// <param name="Operator">A fuzzy operator or openning parenthesis.</param>
        /// 
        /// <returns>A number indicating the priority of the operator, and zero for openning
        /// parenthesis.</returns>
        /// 
        private int Priority( string Operator )
        {
            switch ( Operator )
            {
                case "OR": return 1;
                case "AND": return 2;
            }
            return 0;
        }

        /// <summary>
        /// Converts the Fuzzy Rule to RPN (Reverse Polish Notation). For debug proposes, the string representation of the 
        /// RPN expression can be acessed by calling <see cref="GetRPNExpression"/> method.
        /// </summary>
        /// 
        private void ParseRule( )
        {
            // flag to incicate we are on consequent state
            bool consequent = false;
            
            // tokens like IF and THEN will be searched always in upper case
            string upRule = rule.ToUpper( );

            // the rule must start with IF, and must have a THEN somewhere
            if ( !upRule.StartsWith( "IF" ) )
                throw new ArgumentException( "A Fuzzy Rule must start with an IF statement." );
            if ( upRule.IndexOf( "THEN" ) < 0 )
                throw new ArgumentException( "Missing the consequent (THEN) statement." );

            // building a list with all the expression (rule) string tokens
            string spacedRule = rule.Replace( "(", " ( " ).Replace( ")", " ) " );
            string [] tokensList = spacedRule.Split( ' ' );

            // stack to convert to RPN
            Stack<string> s = new Stack<string>( );
            // storing the last token
            string lastToken = "IF";
            // linguistic var read, used to build clause
            LinguisticVariable lingVar = null;

            // verifying each token
            for ( int i = 0; i < tokensList.Length; i++ )
            {
                // removing spaces
                string token = tokensList[i].Trim( );
                // getting upper case
                string upToken = token.ToUpper( );

                // ignoring these tokens
                if ( upToken == "" || upToken == "IF" ) continue;

                // if the THEN is found, the rule is now on consequent
                if ( upToken == "THEN" )
                {
                    lastToken = upToken;
                    consequent = true;
                    continue;
                }

                // if we got a linguistic variable, an IS statement and a label is needed
                if ( lastToken == "VAR" )
                {
                    if ( upToken == "IS" )
                        lastToken = upToken;
                    else
                        throw new ArgumentException( "An IS statement is expected after a linguistic variable." );
                }
                // if we got an IS statement, a label must follow it
                else if ( lastToken == "IS" )
                {
                    try
                    {
                        FuzzySet fs = lingVar.GetLabel( token );
                        Clause c = new Clause( lingVar, fs );
                        if ( consequent )
                            output = c;
                        else
                            rpnTokenList.Add( c );
                        lastToken = "LAB";
                    }
                    catch ( KeyNotFoundException )
                    {
                        throw new ArgumentException( "Linguistic label " + token + " was not found on the variable " + lingVar.Name + "." );
                    }
                }
                // not VAR and not IS statement 
                else
                {
                    // openning new scope
                    if ( upToken == "(" )
                    {
                        // if we are on consequent, only variables can be found
                        if ( consequent )
                            throw new ArgumentException( "Linguistic variable expected after a THEN statement." );
                        // if its a (, just push it
                        s.Push( upToken );
                        lastToken = upToken;
                    }
                    // operators
                    else if ( upToken == "AND" || upToken == "OR" )
                    {
                        // if we are on consequent, only variables can be found
                        if ( consequent )
                            throw new ArgumentException( "Linguistic variable expected after a THEN statement." );
                        
                        // pop all the higher priority operators until the stack is empty 
                        while ( ( s.Count > 0 ) && ( Priority( s.Peek( ) ) > Priority( upToken ) ) )
                            rpnTokenList.Add( s.Pop( ) );

                        // pushing the operator    
                        s.Push( upToken );
                        lastToken = upToken;
                    }
                    // closing the scope
                    else if ( upToken == ")" )
                    {
                        // if we are on consequent, only variables can be found
                        if ( consequent )
                            throw new ArgumentException( "Linguistic variable expected after a THEN statement." );

                        // if there is nothing on the stack, an oppening parenthesis is missing.
                        if ( s.Count == 0 )
                            throw new ArgumentException( "Openning parenthesis missing." );

                        // pop the tokens and copy to output until openning is found 
                        while ( s.Peek( ) != "(" )
                        {
                            rpnTokenList.Add( s.Pop( ) );
                            if ( s.Count == 0 )
                                throw new ArgumentException( "Openning parenthesis missing." );
                        }
                        s.Pop( );

                        // saving last token...
                        lastToken = upToken;
                    }
                    // finally, the token is a variable
                    else
                    {
                        // find the variable
                        try
                        {
                            lingVar = database.GetVariable( token );
                            lastToken = "VAR";
                        }
                        catch ( KeyNotFoundException )
                        {
                            throw new ArgumentException( "Linguistic variable " + token + " was not found on the database." );
                        }
                    }
                }
            }

            // popping all operators left in stack
            while ( s.Count > 0 )
                rpnTokenList.Add( s.Pop( ) );
        }

        /// <summary>
        /// Evaluates the firing strength of the Rule, the degree of confidence that the consequent of this Rule
        /// must be executed.
        /// </summary>
        /// 
        /// <returns>The firing strength [0..1] of the Rule.</returns>
        /// 
        public double EvaluateFiringStrength( )
        {
            // Stack to store the operand values
            Stack<double> s = new Stack<double>( );

            // Logic to calculate the firing strength
            foreach ( object o in rpnTokenList )
            {
                // if its a clause, then its value must be calculated and pushed
                if ( o.GetType( ) == typeof( Clause ) )
                {
                    Clause c = o as Clause;
                    s.Push( c.Evaluate( ) );
                }
                // if its an operator (AND / OR) the operation is performed and the result 
                // returns to the stack
                else
                {
                    // Operands
                    double y = s.Pop( );
                    double x = s.Pop( );
                    // Operation
                    switch ( o.ToString( ) )
                    {
                        case "AND":
                            s.Push( normOperator.Evaluate( x, y ) );
                            break;
                        case "OR":
                            s.Push( conormOperator.Evaluate( x, y ) );
                            break;
                    }
                }

            }

            // result on the top of stack
            return s.Pop( );
        }
    }
}
