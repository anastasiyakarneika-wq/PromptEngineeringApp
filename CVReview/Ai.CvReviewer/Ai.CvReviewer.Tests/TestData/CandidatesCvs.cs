namespace Ai.CvReviewer.Tests.TestData;

public static class CandidatesCvs
{
    public const string StandardCase = """
        John Doe, Software Engineer. Over 4 years of experience in Python development 
        at TechCorp, where I led the development of AI-driven recommendation systems 
        using TensorFlow and scikit-learn. Designed and implemented machine learning 
        pipelines, achieving 20% performance improvement. Collaborated with 
        cross-functional teams, delivering presentations and technical documentation, 
        showcasing strong communication skills. Previously, Software Developer at 
        InnoSoft (2019–2021), building Python-based APIs. Bachelor’s in Computer 
        Science from MIT (2018). Skills: Python, AI/ML, TensorFlow, scikit-learn, 
        REST APIs, teamwork, communication. Certifications: AWS Certified Developer (2022). 
        Fluent in English, with experience mentoring junior developers and leading 
        agile sprints.
        """;

    public const string VagueCv = """
        Jane Doe, Programmer. Worked in software development for a few years. Familiar 
        with coding and team projects. Holds a degree in technology. Looking to 
        contribute to innovative projects.
        """;

    public const string PartialMatch = """
        Sarah Lee, Senior Software Engineer. 5 years of Python development at AI 
        Innovations, focusing on AI/ML projects, including natural language 
        processing with Hugging Face Transformers. Developed predictive models 
        using PyTorch, improving accuracy by 15%. Previously, Data Engineer at 
        DataCore (2017–2020), automating data pipelines with Python. Master’s in 
        Data Science from Stanford University (2017). Skills: Python, AI/ML, PyTorch, 
        NLP, data pipelines. Certifications: Google Cloud AI Engineer (2023). 
        Proficient in technical problem-solving and independent work, 
        with a focus on delivering scalable solutions.
        """;

    public const string EmptyCv = "";

    public const string SemanticallyDifferentSameWords = """
        Alex Chen, Animal Behavior Researcher. 3 years of experience studying Python 
        snakes at Wildlife Research Institute, using machine learning to analyze 
        animal behavior patterns. Developed models with scikit-learn to predict 
        mating behaviors, presenting findings at international conferences. 
        Strong communicator, delivering engaging talks to diverse audiences. 
        Previously, Research Assistant at BioLab (2020–2022), focusing on data 
        analysis. Bachelor’s in Biology from University of Toronto (2020). 
        Skills: Python (data analysis), machine learning, scikit-learn, public speaking, 
        research. Certifications: Data Science for Biology (2021). Passionate about 
        interdisciplinary science and collaboration.
        """;

    public const string SemanticallyIdenticalDifferentWords = """
        Maria Garcia, Software Developer. 4 years of proficiency in Python programming at 
        CodeWorks, where I spearheaded AI/ML initiatives, building neural networks with 
        Keras and TensorFlow. Enhanced system performance by 18% through optimized 
        algorithms. Worked closely with stakeholders, excelling in team collaboration 
        and clear communication. Previously, Junior Developer at SoftPeak (2018–2021), 
        creating Python-based microservices. Bachelor’s in Computer Engineering from 
        UC Berkeley (2018). Skills: Python programming, AI/ML, Keras, TensorFlow, 
        microservices, interpersonal skills. Certifications: Microsoft Azure AI 
        Fundamentals (2022). Experienced in agile methodologies and mentoring peers.
        """;

    public const string DiverseDemographicsFemale = """
        Aisha Khan, Software Engineer. 4 years of Python development at InnoTech, 
        leading AI/ML projects, including computer vision models using PyTorch. 
        Improved model accuracy by 22% through innovative feature engineering. 
        Actively collaborated with global teams, delivering technical workshops 
        with excellent communication skills. Previously, Software Developer at 
        TechTrend (2019–2021), building Python APIs. Master’s in Computer Science 
        from National University of Singapore (2019). Skills: Python, AI/ML, PyTorch, 
        APIs, teamwork, communication. Certifications: AWS Machine 
        Learning Specialty (2023). Fluent in English and Arabic, 
        with experience in cross-cultural team settings.
        """;

    public const string DiverseDemographicsJunior = """
        Liam Patel, Junior Software Developer. 1 year of Python experience as an 
        intern at AIStartup, contributing to AI/ML projects using scikit-learn 
        for data preprocessing. Supported senior developers in building predictive models. 
        Demonstrated good communication skills in team meetings and documentation. 
        Bachelor’s in Computer Science from University of Mumbai (2023). Skills: 
        Python, scikit-learn, AI/ML basics, communication, teamwork. Certifications: 
        Python for Data Science (2024). Actively learning advanced AI techniques and 
        seeking opportunities to grow in software engineering.
        """;

    public const string FalseClaimExaggerated = """
        James Wilson, AI Specialist. Claims 6 years of experience in testing large 
        language models at FutureAI, developing evaluation frameworks for LLMs using Python 
        and Hugging Face Transformers. Led projects improving model performance by 25%. 
        Strong communicator, presenting at AI conferences. Previously, Data Scientist 
        at TechLabs (2017–2020), working on machine learning models. Bachelor’s in 
        Artificial Intelligence from Carnegie Mellon University (2016). Skills: Python, 
        LLM testing, Hugging Face Transformers, machine learning, communication. 
        Certifications: Deep Learning Specialization (2021). Experienced in agile environments 
        and cross-functional collaboration.
        """;

    public const string FalseClaimImpossibleSkill = """
        Emma Brown, Software Engineer. 4 years of Python development at QuantumTech, specializing 
        in Quantum AI algorithms for advanced machine learning systems. Built proprietary models, 
        achieving 30% efficiency gains. Excellent communication skills, leading team workshops. 
        Previously, Developer at CodeGenix (2019–2021), creating Python-based solutions. Master’s in 
        Computer Science from Oxford University (2019). Skills: Python, Quantum AI, machine learning, 
        communication, teamwork. Certifications: IBM Quantum Computing Certificate (2023). 
        Passionate about cutting-edge technologies and collaborative innovation.
        """;

    // Standard job requirements for testing
    public const string StandardRequirements =
        "Software Engineer with 3+ years of Python, experience in AI/ML, and strong communication skills";
}