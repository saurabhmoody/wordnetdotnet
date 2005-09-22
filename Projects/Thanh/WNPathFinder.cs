/*
 Searching IS_A connection in wordnet
 Author: Thanh Ngoc Dao - Thanh.dao@gmx.net
 Copyright (c) 2005 by Thanh Ngoc Dao.
*/
 
using System.Collections;
using System.Diagnostics;
using System.Threading;

using Wnlib;


namespace WordsMatching
{
	/// <summary>
	/// Summary description for WNDistance.
	/// </summary>
	public class WNPathFinder
	{        		
		const int DEPTH=6;
		static readonly Opt IS_A_NOUN=Opt.at(8); //hypernymy and synonyms  12 FULL TREE
		static readonly Opt IS_A_VERB=Opt.at(31);//troponymy and synonyms

		private string[]  _word=new string[2] ;
		private int[]  _senseIndex=new int[2] ;
	
		private SynSet[] _sense=new SynSet[2] ;
		private Thread[] _thread=new Thread[2] ;
		private ThreadStart[] _threadStart=new ThreadStart[2] ;
		ArrayList[] queue=new ArrayList[2] ;// List of hypernymy for nound and troponymy for verb
		ArrayList[] depth=new ArrayList[2] ;// Depth of node		

		public WNPathFinder()
		{
			//
			// TODO: Add constructor logic here
			//
		}
		private static Hashtable trace=new Hashtable() ;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public int GetPathLength(SynSet s1, SynSet  s2)
		{
			_sense[0]=s1; _sense[1]=s2;
			_word[0]=_sense[0].words[_sense[0].whichword - 1].word ;				
			_word[1]=_sense[1].words[_sense[1].whichword - 1].word ;				
			_senseIndex[0]=_sense[0].words[_sense[0].whichword - 1].wnsns  ; // 0..
			_senseIndex[1]=_sense[1].words[_sense[1].whichword - 1].wnsns  ; // 0..
			int i=Search_IS_A_Connection (IS_A_NOUN);
			if (i == -1)			
				i=Search_IS_A_Connection (IS_A_VERB);
			
			return i;
		}

		public int GetPathLength(string word1,int sense1, string word2, int sense2)
		{
			_word[0]=word1;				
			_word[1]=word2;	
			_senseIndex[0]= sense1;
			_senseIndex[1]= sense2;
			int i=Search_IS_A_Connection (IS_A_NOUN);
			if (i == -1)			
				i=Search_IS_A_Connection (IS_A_VERB);
			return i;			
		}
				                                                                                                                                                                            
		private void Flush()
		{
			for(int i=0; i<2 ; i++)
			{
				queue[i]=new ArrayList() ;				
				depth[i]=new ArrayList() ;				
			}
		}

		private int Search_IS_A_Connection(Opt type)
		{
			Flush();
			int i=Spread(0, type ); //build source tree
			if (i == -1)			
				i=Spread(1, type );	//build target tree and make connection
			return i;
		}

		//search subsumer, a shared parent of two synsets.
		public int FindSubsumer(Lexeme lexe, ArrayList hypernyms)
		{
			for(int i=0; i < hypernyms.Count; i++)
			{
				Lexeme l=(Lexeme)hypernyms[i];
				if (lexe.word == l.word && lexe.wnsns == l.wnsns)
					return i;
			}

			return -1;
		}
		
		private bool IsContain(Lexeme lex, SynSetList senses)
		{						
			foreach (SynSet syn in senses)			
			{				
				foreach (Lexeme l in syn.words)
					if (lex.word == l.word)					
						return true;
					
			}
			return false;
		}
	 
		public int Spread(int index, Opt opt)
		{
			int head=-1, tail=-1; 
			queue[index]=new ArrayList() ;
			depth[index]=new ArrayList() ;
						
			Search se=new Search(_word[index], true, opt.pos , opt.sch, _senseIndex [index]);//

			foreach (object obj in se.lexemes)
			{
				DictionaryEntry dic = (DictionaryEntry) obj;

				Lexeme lex = (Lexeme) dic.Key;
				bool ok=true;

				if (IsContain(lex, se.senses ))
				{
					ok=false;
					if (_word[1 - index] == lex.word)
					{
						Trace.WriteLine("They are synonymy");
						return 1;
					}
				};				

				foreach (Lexeme l in queue[index])
					if (lex.word == l.word) ok = false;

				if (ok && !trace.ContainsKey(lex))
				{
					++tail;
					lex.word = lex.word.Replace("_", " ");
					queue[index].Add(lex);
					depth[index].Add(1);
					trace.Add(lex, 1) ;					
				}
			}


			while (head < tail )
			{
				++head;
				Lexeme lexHead=(Lexeme)queue[index][head];
				int lexHeadDis=(int)depth[index][head]  ;
				if (lexHead.word == _word[1 - index])
				{					
					Trace.WriteLine(lexHead.word + " is hypernym of " + _word[index]) ;					
					return lexHeadDis;
				}
				else
				{
					int subsumer=FindSubsumer(lexHead, queue [1-index]);				
					if (subsumer >= 0)
					{
						int distance=lexHeadDis + (int)depth[1-index][subsumer] ;
						Trace.WriteLine("Hierachy shared common parent found : " + distance + " shared parent :" + lexHead.word) ;
						return distance;
					}
				}	
				se=new Search(lexHead.word, true, opt.pos , opt.sch, lexHead.wnsns); //lexHead.wnsns

				IDictionaryEnumerator enumerator=se.lexemes.GetEnumerator();

				while (enumerator.MoveNext())
				{
					Lexeme lex=(Lexeme) enumerator.Key;
					lex.word=lex.word.Replace("_", " ");

					if ((bool) enumerator.Value)
					{						
						bool ok=true;
						foreach (Lexeme l in queue[index])
							if (lex.word == l.word && lex.wnsns == l.wnsns) ok = false;

						if (ok)
						{
							if (IsContain(lex, se.senses)) ok=false;

							if (!ok)
							{
								++tail;
								depth[index].Add(lexHeadDis);								
								queue[index].Add(lex);

							}
							else if (lex.word != lexHead.word)
							{
								if (!trace.ContainsKey(lex))
								{
									++tail;									
									trace.Add(lex, lexHeadDis + 1) ;
									depth[index].Add(lexHeadDis + 1);									
									queue[index].Add(lex);
								}
							}
						}

					}

				}

			}
			return -1; //not found 
		}

	}
}