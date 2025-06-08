import React, { useState } from 'react';
import { Play, Pause, SkipForward, MessageSquare, Brain, Users, TrendingUp, Search } from 'lucide-react';

const Demo = () => {
  const [currentStep, setCurrentStep] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);

  const demoSteps = [
    {
      title: "Lead Capture & Intelligence",
      subtitle: "Kevin Brown's Website Inquiry",
      content: {
        type: "lead-capture",
        data: {
          contact: "Kevin Brown",
          inquiry: "Hi, I saw your listing for the condo on 5th Avenue. Is it still available? I'm pre-approved up to $500K and looking to move quickly. Can we schedule a showing this weekend? Thanks, Kevin",
          aiAnalysis: {
            urgencySignals: ["move quickly", "pre-approved", "this weekend"],
            budget: "$500K",
            priority: "High",
            nextAction: "Immediate response with showing availability"
          },
          response: "Hi Kevin, Thanks for your interest in the 5th Avenue condo! Yes, it's still available. I'd love to show it to you this weekend. Are you available Saturday at 2 PM or Sunday at 11 AM? I'll also send you some comparable properties that might interest you. Looking forward to hearing from you! Best, Jessica Chen",
          responseTime: "75 minutes"
        }
      }
    },
    {
      title: "Relationship Evolution",
      subtitle: "Emily Johnson: Lead → Prospect → Client",
      content: {
        type: "relationship-timeline",
        data: {
          contact: "Emily Johnson",
          timeline: [
            { date: "Nov 15", status: "Lead", event: "Initial website inquiry", emotion: "curious" },
            { date: "Nov 16", status: "Prospect", event: "First property showing", emotion: "interested" },
            { date: "Nov 18", status: "Prospect", event: "Follow-up call about HOA concerns", emotion: "concerned" },
            { date: "Nov 20", status: "Client", event: "Offer strategy discussion", emotion: "excited-nervous" }
          ],
          currentCall: {
            transcript: "Emily called excited about the Hillcrest condo we saw yesterday. She wants to make an offer but is nervous about competing with other buyers. I discussed offer strategy with her - recommended full price ($525K) with quick close (21 days) and a personal letter to the seller.",
            aiInsights: {
              emotion: "excited-nervous",
              propertyAddress: "456 Hillcrest Ave, San Diego, CA",
              offerAmount: "$525,000",
              competingOffers: "2",
              recommendation: "Full price offer with personal letter to strengthen position"
            }
          }
        }
      }
    },
    {
      title: "Service Provider Orchestration",
      subtitle: "Robert Williams' Luxury Purchase",
      content: {
        type: "service-network",
        data: {
          contact: "Robert Williams",
          property: "123 Ocean View Drive, La Jolla - $1.2M",
          assignments: [
            { provider: "Jennifer Adams", type: "Lender", status: "Active", rating: 5.0, purpose: "Jumbo loan for luxury property" },
            { provider: "Mark Thompson", type: "Inspector", status: "Scheduled", purpose: "Home inspection - Monday 10 AM" },
            { provider: "Rachel Martinez", type: "Title Company", status: "Active", purpose: "Escrow and title services" }
          ],
          coordination: {
            agent: "Amanda Foster",
            timeline: "30-day close",
            buyerType: "Cash with financing option",
            nextSteps: ["Inspection coordination", "Lender documentation", "Final walkthrough"]
          }
        }
      }
    },
    {
      title: "AI-Powered Insights",
      subtitle: "Natural Language Queries",
      content: {
        type: "ai-queries",
        data: {
          queries: [
            {
              question: "Show me all first-time buyers who are concerned about HOA fees",
              result: {
                contacts: ["Emily Johnson"],
                context: "Expressed concerns about $450/month HOA fees during Hillcrest condo showing",
                recommendation: "Send alternative properties with lower HOA fees in University Heights and Normal Heights"
              }
            },
            {
              question: "Which prospects are most likely to make an offer this week?",
              result: {
                contacts: [
                  { name: "Kevin Brown", score: 92, reason: "High urgency signals, pre-approved, weekend showing scheduled" },
                  { name: "James Wilson", score: 88, reason: "Relocation timeline pressure, motivated by job start date" }
                ]
              }
            },
            {
              question: "Find referral opportunities from past clients",
              result: {
                opportunities: [
                  { client: "Thomas Anderson", referral: "Brother Mark looking to relocate from Denver", budget: "Under $600K", warmth: "High" }
                ]
              }
            }
          ]
        }
      }
    },
    {
      title: "The Multiplier Effect",
      subtitle: "Team Collaboration & Scale",
      content: {
        type: "team-overview",
        data: {
          organization: "Premier Realty Homes",
          agents: [
            { name: "Mike Rodriguez", role: "Broker/Owner", contacts: 12, conversion: "22%" },
            { name: "Sarah Johnson", role: "Senior Agent", contacts: 15, conversion: "19%" },
            { name: "Jessica Chen", role: "Agent", contacts: 8, conversion: "25%" },
            { name: "Amanda Foster", role: "Luxury Specialist", contacts: 7, conversion: "15%" },
            { name: "David Kim", role: "Assistant", contacts: 5, conversion: "30%" }
          ],
          totalContacts: 47,
          avgResponseTime: "2.3 hours",
          teamConversion: "18%",
          aiImpact: {
            responseImprovement: "3x faster",
            conversionIncrease: "25%",
            missedFollowups: "Zero"
          }
        }
      }
    }
  ];

  const nextStep = () => {
    if (currentStep < demoSteps.length - 1) {
      setCurrentStep(currentStep + 1);
    }
  };

  const prevStep = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1);
    }
  };

  const togglePlay = () => {
    setIsPlaying(!isPlaying);
    if (!isPlaying) {
      // Auto-advance every 10 seconds when playing
      const interval = setInterval(() => {
        setCurrentStep(prev => {
          if (prev < demoSteps.length - 1) {
            return prev + 1;
          } else {
            setIsPlaying(false);
            clearInterval(interval);
            return prev;
          }
        });
      }, 10000);
    }
  };

  const renderStepContent = (step) => {
    const { content } = step;
    
    switch (content.type) {
      case "lead-capture":
        return (
          <div className="space-y-6">
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
              <h4 className="font-semibold text-blue-900 mb-3">Website Inquiry - 2:30 PM</h4>
              <p className="text-blue-800 italic">"{content.data.inquiry}"</p>
            </div>
            
            <div className="bg-purple-50 border border-purple-200 rounded-lg p-6">
              <h4 className="font-semibold text-purple-900 mb-3 flex items-center">
                <Brain className="h-5 w-5 mr-2" />
                AI Analysis
              </h4>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-sm font-medium text-purple-700">Urgency Signals:</p>
                  <ul className="text-sm text-purple-600">
                    {content.data.aiAnalysis.urgencySignals.map((signal, i) => (
                      <li key={i}>• "{signal}"</li>
                    ))}
                  </ul>
                </div>
                <div>
                  <p className="text-sm font-medium text-purple-700">Priority: <span className="text-red-600">{content.data.aiAnalysis.priority}</span></p>
                  <p className="text-sm font-medium text-purple-700">Budget: {content.data.aiAnalysis.budget}</p>
                </div>
              </div>
            </div>

            <div className="bg-green-50 border border-green-200 rounded-lg p-6">
              <h4 className="font-semibold text-green-900 mb-3">AI-Suggested Response - Sent in {content.data.responseTime}</h4>
              <p className="text-green-800">"{content.data.response}"</p>
            </div>
          </div>
        );

      case "relationship-timeline":
        return (
          <div className="space-y-6">
            <div className="bg-white border rounded-lg p-6">
              <h4 className="font-semibold mb-4">Contact Evolution Timeline</h4>
              <div className="space-y-3">
                {content.data.timeline.map((event, i) => (
                  <div key={i} className="flex items-center space-x-4">
                    <div className={`w-3 h-3 rounded-full ${event.status === 'Client' ? 'bg-green-500' : event.status === 'Prospect' ? 'bg-yellow-500' : 'bg-blue-500'}`}></div>
                    <div className="flex-1">
                      <span className="font-medium">{event.date}</span> - 
                      <span className={`ml-2 px-2 py-1 rounded text-xs ${event.status === 'Client' ? 'bg-green-100 text-green-800' : event.status === 'Prospect' ? 'bg-yellow-100 text-yellow-800' : 'bg-blue-100 text-blue-800'}`}>
                        {event.status}
                      </span>
                      <span className="ml-2 text-gray-600">{event.event}</span>
                      <span className="ml-2 text-sm text-purple-600">({event.emotion})</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
              <h4 className="font-semibold text-yellow-900 mb-3">Latest Call Transcript</h4>
              <p className="text-yellow-800 mb-4">"{content.data.currentCall.transcript}"</p>
              
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <p><strong>Property:</strong> {content.data.currentCall.aiInsights.propertyAddress}</p>
                  <p><strong>Offer Amount:</strong> {content.data.currentCall.aiInsights.offerAmount}</p>
                </div>
                <div>
                  <p><strong>Competing Offers:</strong> {content.data.currentCall.aiInsights.competingOffers}</p>
                  <p><strong>Emotion:</strong> {content.data.currentCall.aiInsights.emotion}</p>
                </div>
              </div>
            </div>
          </div>
        );

      case "service-network":
        return (
          <div className="space-y-6">
            <div className="bg-white border rounded-lg p-6">
              <h4 className="font-semibold mb-4">{content.data.property}</h4>
              <p className="text-gray-600 mb-4">Agent: {content.data.coordination.agent} | Timeline: {content.data.coordination.timeline}</p>
              
              <div className="space-y-3">
                {content.data.assignments.map((assignment, i) => (
                  <div key={i} className="flex items-center justify-between p-3 bg-gray-50 rounded">
                    <div>
                      <p className="font-medium">{assignment.provider}</p>
                      <p className="text-sm text-gray-600">{assignment.type} - {assignment.purpose}</p>
                    </div>
                    <div className="text-right">
                      <span className={`px-2 py-1 rounded text-xs ${assignment.status === 'Active' ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'}`}>
                        {assignment.status}
                      </span>
                      {assignment.rating && (
                        <p className="text-sm text-yellow-600 mt-1">★ {assignment.rating}</p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        );

      case "ai-queries":
        return (
          <div className="space-y-6">
            {content.data.queries.map((query, i) => (
              <div key={i} className="bg-white border rounded-lg p-6">
                <div className="flex items-center space-x-2 mb-4">
                  <Search className="h-5 w-5 text-blue-500" />
                  <p className="font-medium text-blue-900">"{query.question}"</p>
                </div>
                
                {query.result.contacts && (
                  <div className="bg-blue-50 p-4 rounded">
                    <p className="font-medium">Results:</p>
                    {Array.isArray(query.result.contacts) ? (
                      query.result.contacts.map((contact, j) => (
                        <div key={j} className="mt-2">
                          {typeof contact === 'string' ? (
                            <p>• {contact}</p>
                          ) : (
                            <p>• {contact.name} (Score: {contact.score}%) - {contact.reason}</p>
                          )}
                        </div>
                      ))
                    ) : (
                      <p>• {query.result.contacts}</p>
                    )}
                    {query.result.context && (
                      <p className="mt-2 text-sm text-gray-600">Context: {query.result.context}</p>
                    )}
                    {query.result.recommendation && (
                      <p className="mt-2 text-sm text-blue-600">💡 {query.result.recommendation}</p>
                    )}
                  </div>
                )}

                {query.result.opportunities && (
                  <div className="bg-green-50 p-4 rounded">
                    <p className="font-medium">Referral Opportunities:</p>
                    {query.result.opportunities.map((opp, j) => (
                      <div key={j} className="mt-2">
                        <p>• <strong>{opp.client}</strong>: {opp.referral}</p>
                        <p className="text-sm text-gray-600">Budget: {opp.budget} | Warmth: {opp.warmth}</p>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        );

      case "team-overview":
        return (
          <div className="space-y-6">
            <div className="grid grid-cols-3 gap-4 mb-6">
              <div className="bg-blue-100 p-4 rounded text-center">
                <p className="text-2xl font-bold text-blue-800">{content.data.totalContacts}</p>
                <p className="text-blue-600">Total Contacts</p>
              </div>
              <div className="bg-green-100 p-4 rounded text-center">
                <p className="text-2xl font-bold text-green-800">{content.data.teamConversion}</p>
                <p className="text-green-600">Team Conversion</p>
              </div>
              <div className="bg-purple-100 p-4 rounded text-center">
                <p className="text-2xl font-bold text-purple-800">{content.data.avgResponseTime}</p>
                <p className="text-purple-600">Avg Response Time</p>
              </div>
            </div>

            <div className="bg-white border rounded-lg p-6">
              <h4 className="font-semibold mb-4">Team Performance</h4>
              <div className="space-y-3">
                {content.data.agents.map((agent, i) => (
                  <div key={i} className="flex items-center justify-between p-3 bg-gray-50 rounded">
                    <div>
                      <p className="font-medium">{agent.name}</p>
                      <p className="text-sm text-gray-600">{agent.role}</p>
                    </div>
                    <div className="text-right">
                      <p className="font-medium">{agent.contacts} contacts</p>
                      <p className="text-sm text-green-600">{agent.conversion} conversion</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="bg-gradient-to-r from-emma-blue to-purple-600 rounded-lg p-6 text-white">
              <h4 className="font-semibold mb-4">AI Impact</h4>
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <p className="text-2xl font-bold">{content.data.aiImpact.responseImprovement}</p>
                  <p className="text-sm opacity-90">Faster Responses</p>
                </div>
                <div>
                  <p className="text-2xl font-bold">{content.data.aiImpact.conversionIncrease}</p>
                  <p className="text-sm opacity-90">Higher Conversion</p>
                </div>
                <div>
                  <p className="text-2xl font-bold">{content.data.aiImpact.missedFollowups}</p>
                  <p className="text-sm opacity-90">Missed Follow-ups</p>
                </div>
              </div>
            </div>
          </div>
        );

      default:
        return <div>Content not available</div>;
    }
  };

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="text-center space-y-4">
        <h1 className="text-4xl font-bold text-emma-dark">EMMA AI Live Demo</h1>
        <p className="text-xl text-gray-600">Experience the Future of Real Estate Intelligence</p>
        
        {/* Demo Controls */}
        <div className="flex items-center justify-center space-x-4">
          <button
            onClick={prevStep}
            disabled={currentStep === 0}
            className="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg disabled:opacity-50"
          >
            Previous
          </button>
          
          <button
            onClick={togglePlay}
            className="flex items-center space-x-2 px-6 py-3 bg-emma-blue text-white rounded-lg hover:bg-blue-600"
          >
            {isPlaying ? <Pause className="h-5 w-5" /> : <Play className="h-5 w-5" />}
            <span>{isPlaying ? 'Pause' : 'Play'} Demo</span>
          </button>
          
          <button
            onClick={nextStep}
            disabled={currentStep === demoSteps.length - 1}
            className="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg disabled:opacity-50"
          >
            Next
          </button>
        </div>

        {/* Progress Bar */}
        <div className="w-full bg-gray-200 rounded-full h-2">
          <div 
            className="bg-emma-blue h-2 rounded-full transition-all duration-300"
            style={{ width: `${((currentStep + 1) / demoSteps.length) * 100}%` }}
          ></div>
        </div>
        <p className="text-sm text-gray-500">Step {currentStep + 1} of {demoSteps.length}</p>
      </div>

      {/* Current Step */}
      <div className="bg-white rounded-lg card-shadow p-8">
        <div className="text-center mb-8">
          <h2 className="text-3xl font-bold text-emma-dark mb-2">
            {demoSteps[currentStep].title}
          </h2>
          <p className="text-lg text-gray-600">
            {demoSteps[currentStep].subtitle}
          </p>
        </div>

        {renderStepContent(demoSteps[currentStep])}
      </div>

      {/* Navigation Dots */}
      <div className="flex justify-center space-x-2">
        {demoSteps.map((_, index) => (
          <button
            key={index}
            onClick={() => setCurrentStep(index)}
            className={`w-3 h-3 rounded-full transition-colors ${
              index === currentStep ? 'bg-emma-blue' : 'bg-gray-300'
            }`}
          />
        ))}
      </div>
    </div>
  );
};

export default Demo;
